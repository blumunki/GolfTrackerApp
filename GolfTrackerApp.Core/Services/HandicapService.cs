using System.Text.Json;
using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Core.Services;

public class HandicapService : IHandicapService
{
    private const int QualifyingHoles = 18;

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<HandicapService> _logger;

    public HandicapService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<HandicapService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<int> OnRoundCompletedAsync(int roundId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var round = await context.Rounds
            .Include(r => r.RoundPlayers)
            .Include(r => r.Scores)
                .ThenInclude(s => s.Hole)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.RoundId == roundId);

        if (round is null
            || round.Status != RoundCompletionStatus.Completed
            || round.HolesPlayed != QualifyingHoles)
        {
            return 0;
        }

        // Load the course's tee sets once so a round with no explicit tee selection
        // (every historical round — TeeSetId is null) can fall back to the course default.
        var courseTeeSets = await context.TeeSets
            .Where(ts => ts.GolfCourseId == round.GolfCourseId)
            .ToListAsync();

        var differentialsWritten = 0;
        foreach (var roundPlayer in round.RoundPlayers)
        {
            var scores = round.Scores
                .Where(s => s.PlayerId == roundPlayer.PlayerId && s.Hole != null)
                .ToList();
            if (scores.Count != QualifyingHoles)
            {
                continue; // incomplete scorecard — not qualifying
            }

            // Tee selection lives on RoundPlayer; scores carry a denormalised copy. When
            // neither is set (historical rounds), use the course default — "null tee means
            // default tees" (ARCHITECTURE §12.5). No round data is mutated.
            var explicitTeeId = roundPlayer.TeeSetId
                ?? scores.Select(s => s.TeeSetId).FirstOrDefault(t => t.HasValue);
            var teeSet = explicitTeeId is not null
                ? courseTeeSets.FirstOrDefault(ts => ts.TeeSetId == explicitTeeId.Value)
                : ResolveDefaultTeeSet(courseTeeSets);

            if (teeSet is not { CourseRating: decimal rating, SlopeRating: int slope })
            {
                continue; // no tee, or default tee has no rating/slope — not qualifying
            }

            // Adjusted gross uses Hole.Par (not tee-specific par), matching the rest of the app.
            // Proper WHS caps each hole at net double bogey, which needs the player's course
            // handicap, which needs an index — so use the index established by this player's
            // EARLIER rounds. Before an index exists, fall back to the par+5 newcomer cap.
            var priorIndex = await GetPriorIndexAsync(context, roundPlayer.PlayerId, round.DatePlayed);
            int adjustedGross;
            if (priorIndex is decimal index)
            {
                var coursePar = scores.Sum(s => s.Hole!.Par);
                var courseHandicap = WhsCalculator.ComputeCourseHandicap(index, slope, rating, coursePar);
                adjustedGross = WhsCalculator.ComputeAdjustedGrossScore(
                    scores.Select(s => (s.Hole!.Par, s.Strokes, s.Hole!.StrokeIndex ?? WhsCalculator.RoundHoles)),
                    courseHandicap);
            }
            else
            {
                adjustedGross = WhsCalculator.ComputeAdjustedGrossScore(
                    scores.Select(s => (s.Hole!.Par, s.Strokes)));
            }
            var differential = WhsCalculator.ComputeDifferential(adjustedGross, rating, slope);

            var existing = await context.ScoringDifferentials
                .FirstOrDefaultAsync(d => d.PlayerId == roundPlayer.PlayerId && d.RoundId == roundId);
            if (existing is null)
            {
                existing = new ScoringDifferential
                {
                    PlayerId = roundPlayer.PlayerId,
                    RoundId = roundId,
                };
                context.ScoringDifferentials.Add(existing);
            }

            existing.TeeSetId = teeSet.TeeSetId;
            existing.AdjustedGrossScore = adjustedGross;
            existing.CourseRating = rating;
            existing.SlopeRating = slope;
            existing.Differential = differential;
            existing.CalculatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            differentialsWritten++;

            await RecalculatePersonalIndexAsync(context, roundPlayer.PlayerId, round.DatePlayed);
        }

        return differentialsWritten;
    }

    /// <summary>
    /// The player's personal index established by rounds played strictly before
    /// <paramref name="beforeDate"/> (the index "going into" this round, used for net double
    /// bogey). Null until three earlier differentials exist. Processing oldest-first keeps
    /// this correct during a backfill.
    /// </summary>
    private async Task<decimal?> GetPriorIndexAsync(ApplicationDbContext context, int playerId, DateTime beforeDate)
    {
        var priorDifferentials = await context.ScoringDifferentials
            .Where(d => d.PlayerId == playerId && d.Round!.DatePlayed < beforeDate)
            .OrderByDescending(d => d.Round!.DatePlayed)
                .ThenByDescending(d => d.RoundId)
            .Select(d => d.Differential)
            .Take(WhsCalculator.DifferentialWindow)
            .ToListAsync();
        return WhsCalculator.ComputeIndex(priorDifferentials);
    }

    /// <summary>
    /// The course's default tee for rounds with no explicit tee selection: "Yellow" (the
    /// documented default that historical rounds map to), else the lowest-sort-order tee.
    /// </summary>
    private static TeeSet? ResolveDefaultTeeSet(IReadOnlyList<TeeSet> courseTeeSets) =>
        courseTeeSets.FirstOrDefault(ts => ts.Name.Equals("Yellow", StringComparison.OrdinalIgnoreCase))
        ?? courseTeeSets.OrderBy(ts => ts.SortOrder).ThenBy(ts => ts.TeeSetId).FirstOrDefault();

    public async Task<HandicapBackfillResult> BackfillPersonalHandicapsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Oldest first so HandicapRecord history evolves in playing order.
        var roundIds = await context.Rounds
            .Where(r => r.Status == RoundCompletionStatus.Completed)
            .OrderBy(r => r.DatePlayed)
                .ThenBy(r => r.RoundId)
            .Select(r => r.RoundId)
            .ToListAsync();

        var recordsBefore = await context.HandicapRecords
            .CountAsync(h => h.Source == HandicapSource.Personal);

        var result = new HandicapBackfillResult { RoundsProcessed = roundIds.Count };
        foreach (var roundId in roundIds)
        {
            var written = await OnRoundCompletedAsync(roundId);
            if (written > 0)
            {
                result.RoundsQualified++;
            }
            result.DifferentialsWritten += written;
        }

        result.HandicapRecordsCreated = await context.HandicapRecords
            .CountAsync(h => h.Source == HandicapSource.Personal) - recordsBefore;
        result.PlayersWithIndex = await context.HandicapRecords
            .Where(h => h.Source == HandicapSource.Personal)
            .Select(h => h.PlayerId)
            .Distinct()
            .CountAsync();

        _logger.LogInformation(
            "Handicap backfill: {Qualified} of {Processed} rounds qualified, {Differentials} differentials, {Records} new records.",
            result.RoundsQualified, result.RoundsProcessed, result.DifferentialsWritten, result.HandicapRecordsCreated);

        return result;
    }

    public async Task<List<HandicapRecord>> GetHandicapRecordsAsync(int playerId, HandicapSource? source = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.HandicapRecords
            .AsNoTracking()
            .Include(h => h.GolfClub)
            .Include(h => h.GolfSociety)
            .Where(h => h.PlayerId == playerId);
        if (source is not null)
        {
            query = query.Where(h => h.Source == source);
        }
        return await query
            .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.HandicapRecordId)
            .ToListAsync();
    }

    public async Task<List<HandicapRecord>> GetActiveHandicapsAsync(int playerId)
    {
        var records = await GetHandicapRecordsAsync(playerId);
        return records
            .GroupBy(h => (h.Source, h.GolfClubId, h.GolfSocietyId))
            .Select(g => g.First()) // records are already newest first
            .OrderBy(h => h.Source)
            .ToList();
    }

    public async Task<List<ScoringDifferential>> GetRecentDifferentialsAsync(int playerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ScoringDifferentials
            .AsNoTracking()
            .Include(d => d.Round)
                .ThenInclude(r => r!.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
            .Include(d => d.TeeSet)
            .Where(d => d.PlayerId == playerId)
            .OrderByDescending(d => d.Round!.DatePlayed)
                .ThenByDescending(d => d.RoundId)
            .Take(WhsCalculator.DifferentialWindow)
            .ToListAsync();
    }

    public async Task<List<HandicapRoundQualification>> GetRoundQualificationsAsync(int playerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var rounds = await context.Rounds
            .AsNoTracking()
            .Where(r => r.Status == RoundCompletionStatus.Completed
                        && r.RoundPlayers.Any(rp => rp.PlayerId == playerId))
            .Include(r => r.GolfCourse)
                .ThenInclude(gc => gc!.GolfClub)
            .Include(r => r.RoundPlayers)
            .OrderByDescending(r => r.DatePlayed)
                .ThenByDescending(r => r.RoundId)
            .ToListAsync();

        if (rounds.Count == 0)
        {
            return new List<HandicapRoundQualification>();
        }

        var roundIds = rounds.Select(r => r.RoundId).ToList();
        var courseIds = rounds.Select(r => r.GolfCourseId).Distinct().ToList();

        // Source of truth for "qualified" is the differential itself — so this view can never
        // disagree with the computed index. Reasons are only derived for the excluded rounds.
        var qualifiedRoundIds = (await context.ScoringDifferentials
            .Where(d => d.PlayerId == playerId && roundIds.Contains(d.RoundId))
            .Select(d => d.RoundId)
            .ToListAsync()).ToHashSet();

        var teeSetsByCourse = (await context.TeeSets
            .Where(ts => courseIds.Contains(ts.GolfCourseId))
            .ToListAsync())
            .GroupBy(ts => ts.GolfCourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TeeSet>)g.ToList());

        var scoreCounts = (await context.Scores
            .Where(s => s.PlayerId == playerId && roundIds.Contains(s.RoundId))
            .GroupBy(s => s.RoundId)
            .Select(g => new { RoundId = g.Key, Count = g.Count() })
            .ToListAsync())
            .ToDictionary(x => x.RoundId, x => x.Count);

        return rounds.Select(r => new HandicapRoundQualification
        {
            RoundId = r.RoundId,
            DatePlayed = r.DatePlayed,
            ClubName = r.GolfCourse?.GolfClub?.Name ?? string.Empty,
            CourseName = r.GolfCourse?.Name ?? "Unknown course",
            HolesPlayed = r.HolesPlayed,
            Status = ClassifyRound(r, playerId, qualifiedRoundIds, teeSetsByCourse, scoreCounts),
        }).ToList();
    }

    private static HandicapRoundStatus ClassifyRound(
        Round round, int playerId, HashSet<int> qualifiedRoundIds,
        IReadOnlyDictionary<int, IReadOnlyList<TeeSet>> teeSetsByCourse,
        IReadOnlyDictionary<int, int> scoreCounts)
    {
        if (qualifiedRoundIds.Contains(round.RoundId))
        {
            return HandicapRoundStatus.Qualified;
        }

        if (round.HolesPlayed != QualifyingHoles)
        {
            return HandicapRoundStatus.NotEighteenHoles;
        }

        var teeSets = teeSetsByCourse.GetValueOrDefault(round.GolfCourseId) ?? Array.Empty<TeeSet>();
        var roundPlayer = round.RoundPlayers.FirstOrDefault(rp => rp.PlayerId == playerId);
        var teeSet = roundPlayer?.TeeSetId is int teeId
            ? teeSets.FirstOrDefault(ts => ts.TeeSetId == teeId)
            : ResolveDefaultTeeSet(teeSets);
        if (teeSet is not { CourseRating: not null, SlopeRating: not null })
        {
            return HandicapRoundStatus.NoCourseRatingSlope;
        }

        if (scoreCounts.GetValueOrDefault(round.RoundId) != QualifyingHoles)
        {
            return HandicapRoundStatus.IncompleteScorecard;
        }

        // 18 holes, rated tee, full card, but no differential — treat as incomplete data.
        return HandicapRoundStatus.IncompleteScorecard;
    }

    public async Task<HandicapRecord> AddManualClubHandicapAsync(HandicapRecord record)
    {
        ValidateManualEntry(record);
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!await context.Players.AnyAsync(p => p.PlayerId == record.PlayerId))
        {
            throw new ArgumentException($"Player with ID {record.PlayerId} does not exist.");
        }
        if (!await context.GolfClubs.AnyAsync(c => c.GolfClubId == record.GolfClubId))
        {
            throw new ArgumentException($"GolfClub with ID {record.GolfClubId} does not exist.");
        }

        record.Source = HandicapSource.ClubRegional;
        record.IsManualEntry = true;
        record.GolfSocietyId = null;
        record.CreatedAt = DateTime.UtcNow;
        context.HandicapRecords.Add(record);
        await context.SaveChangesAsync();

        await RefreshClubDisplayHandicapAsync(context, record.PlayerId);
        return record;
    }

    public async Task<HandicapRecord?> UpdateManualClubHandicapAsync(HandicapRecord record)
    {
        ValidateManualEntry(record);
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.HandicapRecords.FindAsync(record.HandicapRecordId);
        if (existing is null)
        {
            return null;
        }
        EnsureIsManualClubEntry(existing);

        if (existing.GolfClubId != record.GolfClubId
            && !await context.GolfClubs.AnyAsync(c => c.GolfClubId == record.GolfClubId))
        {
            throw new ArgumentException($"GolfClub with ID {record.GolfClubId} does not exist.");
        }

        existing.HandicapIndex = record.HandicapIndex;
        existing.GolfClubId = record.GolfClubId;
        existing.EffectiveDate = record.EffectiveDate;
        existing.ExpiryDate = record.ExpiryDate;
        await context.SaveChangesAsync();

        await RefreshClubDisplayHandicapAsync(context, existing.PlayerId);
        return existing;
    }

    public async Task<bool> DeleteManualClubHandicapAsync(int handicapRecordId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.HandicapRecords.FindAsync(handicapRecordId);
        if (existing is null)
        {
            return false;
        }
        EnsureIsManualClubEntry(existing);

        context.HandicapRecords.Remove(existing);
        await context.SaveChangesAsync();

        await RefreshClubDisplayHandicapAsync(context, existing.PlayerId);
        return true;
    }

    public async Task<Player?> SetPrimaryHandicapSourceAsync(int playerId, HandicapSource? source)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var player = await context.Players.FindAsync(playerId);
        if (player is null)
        {
            return null;
        }

        player.PrimaryHandicapSource = source;
        if (source is HandicapSource newSource)
        {
            var latest = await context.HandicapRecords
                .Where(h => h.PlayerId == playerId && h.Source == newSource)
                .OrderByDescending(h => h.EffectiveDate)
                    .ThenByDescending(h => h.HandicapRecordId)
                .FirstOrDefaultAsync();
            player.Handicap = latest is null ? null : (double)latest.HandicapIndex;
        }
        // source == null: legacy manual mode — Player.Handicap stays as entered.

        await context.SaveChangesAsync();
        return player;
    }

    private static void ValidateManualEntry(HandicapRecord record)
    {
        if (record.GolfClubId is null)
        {
            throw new ArgumentException("A club handicap requires a GolfClubId.");
        }
        if (record.HandicapIndex is < -10.0m or > WhsCalculator.MaxIndex)
        {
            throw new ArgumentException($"Handicap index must be between -10.0 and {WhsCalculator.MaxIndex}.");
        }
        if (record.EffectiveDate == default)
        {
            throw new ArgumentException("An effective date is required.");
        }
    }

    private static void EnsureIsManualClubEntry(HandicapRecord record)
    {
        if (!record.IsManualEntry || record.Source != HandicapSource.ClubRegional)
        {
            throw new InvalidOperationException(
                $"HandicapRecord {record.HandicapRecordId} is a calculated {record.Source} record and cannot be modified manually.");
        }
    }

    /// <summary>Keeps Player.Handicap in sync when the player's primary source is ClubRegional.</summary>
    private static async Task RefreshClubDisplayHandicapAsync(ApplicationDbContext context, int playerId)
    {
        var player = await context.Players.FindAsync(playerId);
        if (player is not { PrimaryHandicapSource: HandicapSource.ClubRegional })
        {
            return;
        }

        var latest = await context.HandicapRecords
            .Where(h => h.PlayerId == playerId && h.Source == HandicapSource.ClubRegional)
            .OrderByDescending(h => h.EffectiveDate)
                .ThenByDescending(h => h.HandicapRecordId)
            .FirstOrDefaultAsync();

        player.Handicap = latest is null ? null : (double)latest.HandicapIndex;
        await context.SaveChangesAsync();
    }

    private async Task RecalculatePersonalIndexAsync(ApplicationDbContext context, int playerId, DateTime effectiveDate)
    {
        var window = await context.ScoringDifferentials
            .Where(d => d.PlayerId == playerId)
            .OrderByDescending(d => d.Round!.DatePlayed)
                .ThenByDescending(d => d.RoundId)
            .Take(WhsCalculator.DifferentialWindow)
            .ToListAsync();

        var index = WhsCalculator.ComputeIndex(window.Select(d => d.Differential));

        // Flag which differentials count towards the index (lowest k of the window).
        var countingIds = window
            .OrderBy(d => d.Differential)
            .Take(WhsCalculator.CountingDifferentialsCount(window.Count))
            .Select(d => d.ScoringDifferentialId)
            .ToHashSet();
        foreach (var d in window)
        {
            d.IsUsedInCalculation = countingIds.Contains(d.ScoringDifferentialId);
        }

        var windowIds = window.Select(d => d.ScoringDifferentialId).ToList();
        var staleFlags = await context.ScoringDifferentials
            .Where(d => d.PlayerId == playerId && d.IsUsedInCalculation && !windowIds.Contains(d.ScoringDifferentialId))
            .ToListAsync();
        foreach (var d in staleFlags)
        {
            d.IsUsedInCalculation = false;
        }

        if (index is decimal newIndex)
        {
            var latest = await context.HandicapRecords
                .Where(h => h.PlayerId == playerId && h.Source == HandicapSource.Personal)
                .OrderByDescending(h => h.EffectiveDate)
                    .ThenByDescending(h => h.HandicapRecordId)
                .FirstOrDefaultAsync();

            if (latest is null || latest.HandicapIndex != newIndex)
            {
                context.HandicapRecords.Add(new HandicapRecord
                {
                    PlayerId = playerId,
                    HandicapIndex = newIndex,
                    Source = HandicapSource.Personal,
                    EffectiveDate = effectiveDate,
                    IsManualEntry = false,
                    CalculationDetails = JsonSerializer.Serialize(new
                    {
                        differentials = window.Select(d => d.Differential),
                        counting = countingIds.Count,
                    }),
                });
                _logger.LogInformation(
                    "Player {PlayerId} personal handicap index changed to {Index}.", playerId, newIndex);

                var player = await context.Players.FindAsync(playerId);
                if (player is { PrimaryHandicapSource: HandicapSource.Personal })
                {
                    player.Handicap = (double)newIndex;
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
