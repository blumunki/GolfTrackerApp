using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Core.Services;

public class CompetitionService : ICompetitionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CompetitionService> _logger;

    public CompetitionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CompetitionService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Competition> CreateCompetitionAsync(Competition competition)
    {
        if (string.IsNullOrWhiteSpace(competition.Name))
        {
            throw new ArgumentException("A competition name is required.");
        }
        if (competition.Date == default)
        {
            throw new ArgumentException("A competition date is required.");
        }
        if (string.IsNullOrWhiteSpace(competition.CreatedByUserId))
        {
            throw new InvalidOperationException("Competition must have a CreatedByUserId.");
        }
        if (competition.GolfClubId is not null && competition.GolfSocietyId is not null)
        {
            throw new ArgumentException("A competition is hosted by a club or a society, not both.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (competition.GolfClubId is int clubId
            && !await context.GolfClubs.AnyAsync(c => c.GolfClubId == clubId))
        {
            throw new ArgumentException($"GolfClub with ID {clubId} does not exist.");
        }
        if (competition.GolfSocietyId is int societyId
            && !await context.GolfSocieties.AnyAsync(s => s.GolfSocietyId == societyId))
        {
            throw new ArgumentException($"GolfSociety with ID {societyId} does not exist.");
        }
        if (competition.GolfCourseId is int courseId
            && !await context.GolfCourses.AnyAsync(c => c.GolfCourseId == courseId))
        {
            throw new ArgumentException($"GolfCourse with ID {courseId} does not exist.");
        }

        competition.CreatedAt = DateTime.UtcNow;
        context.Competitions.Add(competition);
        await context.SaveChangesAsync();
        _logger.LogInformation("Created Competition {CompetitionId} '{Name}'.", competition.CompetitionId, competition.Name);
        return competition;
    }

    public async Task<Competition?> GetCompetitionByIdAsync(int competitionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Competitions
            .AsNoTracking()
            .Include(c => c.GolfClub)
            .Include(c => c.GolfSociety)
            .Include(c => c.GolfCourse)
            .Include(c => c.Entries)
                .ThenInclude(e => e.Player)
            .Include(c => c.Entries)
                .ThenInclude(e => e.TeeSet)
            .Include(c => c.Rounds)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId);
    }

    public async Task<List<Competition>> GetCompetitionsAsync(
        int? golfClubId = null, int? golfSocietyId = null, CompetitionStatus? status = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Competitions
            .AsNoTracking()
            .Include(c => c.GolfClub)
            .Include(c => c.GolfSociety)
            .Include(c => c.GolfCourse)
            .AsQueryable();

        if (golfClubId is not null) query = query.Where(c => c.GolfClubId == golfClubId);
        if (golfSocietyId is not null) query = query.Where(c => c.GolfSocietyId == golfSocietyId);
        if (status is not null) query = query.Where(c => c.Status == status);

        return await query
            .OrderByDescending(c => c.Date)
                .ThenByDescending(c => c.CompetitionId)
            .ToListAsync();
    }

    public async Task<Competition?> UpdateCompetitionAsync(Competition competition)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Competitions.FindAsync(competition.CompetitionId);
        if (existing is null) return null;
        EnsureNotTerminal(existing);

        if (string.IsNullOrWhiteSpace(competition.Name))
        {
            throw new ArgumentException("A competition name is required.");
        }
        if (existing.GolfCourseId != competition.GolfCourseId
            && competition.GolfCourseId is int courseId
            && !await context.GolfCourses.AnyAsync(c => c.GolfCourseId == courseId))
        {
            throw new ArgumentException($"GolfCourse with ID {courseId} does not exist.");
        }

        existing.Name = competition.Name;
        existing.ScoringFormat = competition.ScoringFormat;
        existing.Date = competition.Date;
        existing.Description = competition.Description;
        existing.IsOpen = competition.IsOpen;
        existing.GolfCourseId = competition.GolfCourseId;
        await context.SaveChangesAsync();
        return existing;
    }

    public async Task<Competition?> SetStatusAsync(int competitionId, CompetitionStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var competition = await context.Competitions.FindAsync(competitionId);
        if (competition is null) return null;

        if (competition.Status != status)
        {
            EnsureNotTerminal(competition);
            competition.Status = status;
            await context.SaveChangesAsync();
            _logger.LogInformation("Competition {CompetitionId} status set to {Status}.", competitionId, status);
        }
        return competition;
    }

    public async Task<CompetitionEntry> AddEntryAsync(int competitionId, int playerId, int? teeSetId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var competition = await context.Competitions.FindAsync(competitionId)
            ?? throw new ArgumentException($"Competition with ID {competitionId} does not exist.");
        EnsureNotTerminal(competition);

        var player = await context.Players.FindAsync(playerId)
            ?? throw new ArgumentException($"Player with ID {playerId} does not exist.");
        if (teeSetId is int tee && !await context.TeeSets.AnyAsync(ts => ts.TeeSetId == tee))
        {
            throw new ArgumentException($"TeeSet with ID {tee} does not exist.");
        }
        if (await context.CompetitionEntries.AnyAsync(
                e => e.CompetitionId == competitionId && e.PlayerId == playerId))
        {
            throw new InvalidOperationException(
                $"Player {playerId} is already entered in competition {competitionId}.");
        }

        var entry = new CompetitionEntry
        {
            CompetitionId = competitionId,
            PlayerId = playerId,
            TeeSetId = teeSetId,
            // Snapshot the display handicap at entry time — results stay explainable even
            // if the player's handicap moves before the competition completes.
            HandicapAtEntry = player.Handicap is double handicap ? (decimal)handicap : null,
        };
        context.CompetitionEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> RemoveEntryAsync(int competitionId, int playerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entry = await context.CompetitionEntries.FirstOrDefaultAsync(
            e => e.CompetitionId == competitionId && e.PlayerId == playerId);
        if (entry is null) return false;

        var competition = await context.Competitions.FindAsync(competitionId);
        if (competition is { Status: CompetitionStatus.Completed })
        {
            throw new InvalidOperationException("Entries cannot be withdrawn from a completed competition.");
        }

        context.CompetitionEntries.Remove(entry);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<Round?> AssignRoundAsync(int roundId, int competitionId, string requestingUserId, bool isUserAdmin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var round = await context.Rounds.FindAsync(roundId);
        if (round is null) return null;
        var competition = await context.Competitions.FindAsync(competitionId);
        if (competition is null) return null;
        if (competition.Status == CompetitionStatus.Cancelled)
        {
            throw new InvalidOperationException("Rounds cannot be assigned to a cancelled competition.");
        }
        EnsureRoundOwnership(round, requestingUserId, isUserAdmin);

        // RoundType (Friendly/Competitive) is deliberately untouched — the competition link
        // is independent of the historical classification (ARCHITECTURE §12.5 3.5).
        round.CompetitionId = competitionId;
        await context.SaveChangesAsync();
        return round;
    }

    public async Task<Round?> UnassignRoundAsync(int roundId, string requestingUserId, bool isUserAdmin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var round = await context.Rounds.FindAsync(roundId);
        if (round is null) return null;
        EnsureRoundOwnership(round, requestingUserId, isUserAdmin);

        round.CompetitionId = null;
        await context.SaveChangesAsync();
        return round;
    }

    public async Task<List<Competition>> GetCompetitionsForPlayerAsync(int playerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Competitions
            .AsNoTracking()
            .Include(c => c.GolfClub)
            .Include(c => c.GolfSociety)
            .Where(c => (c.Status == CompetitionStatus.Upcoming || c.Status == CompetitionStatus.InProgress)
                        && c.Entries.Any(e => e.PlayerId == playerId))
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<bool> IsHostManagerAsync(int? golfClubId, int? golfSocietyId, string userId)
    {
        if (golfSocietyId is not int societyId)
        {
            return false; // club-hosted (until 3-8) and ad-hoc: global admin only
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SocietyMemberships.AnyAsync(m =>
            m.GolfSocietyId == societyId
            && m.UserId == userId
            && (m.Role == MembershipRole.Admin || m.Role == MembershipRole.Owner));
    }

    public async Task<List<CompetitionEntry>> ComputeResultsAsync(int competitionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var competition = await context.Competitions
            .Include(c => c.Entries)
            .Include(c => c.Rounds)
                .ThenInclude(r => r.Scores)
                    .ThenInclude(s => s.Hole)
            .Include(c => c.Rounds)
                .ThenInclude(r => r.RoundPlayers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.CompetitionId == competitionId)
            ?? throw new ArgumentException($"Competition with ID {competitionId} does not exist.");

        var courseIds = competition.Rounds.Select(r => r.GolfCourseId).Distinct().ToList();
        var teeSetsByCourse = (await context.TeeSets
            .Where(ts => courseIds.Contains(ts.GolfCourseId))
            .ToListAsync())
            .GroupBy(ts => ts.GolfCourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TeeSet>)g.ToList());
        var allTeeSets = teeSetsByCourse.Values.SelectMany(t => t).ToList();

        foreach (var entry in competition.Entries)
        {
            var round = competition.Rounds.FirstOrDefault(r =>
                r.Scores.Any(s => s.PlayerId == entry.PlayerId));
            if (round is null)
            {
                entry.GrossScore = null;
                entry.NetScore = null;
                entry.StablefordPoints = null;
                entry.Position = null;
                continue;
            }

            var scores = round.Scores
                .Where(s => s.PlayerId == entry.PlayerId && s.Hole != null)
                .ToList();
            entry.GrossScore = scores.Sum(s => s.Strokes);

            // Course handicap from the entry's snapshot and the tee played (entry tee →
            // round tee → course default). Scratch when handicap or rating/slope missing.
            var roundPlayer = round.RoundPlayers.FirstOrDefault(rp => rp.PlayerId == entry.PlayerId);
            var teeSetId = entry.TeeSetId ?? roundPlayer?.TeeSetId;
            var teeSet = teeSetId is int tee
                ? allTeeSets.FirstOrDefault(ts => ts.TeeSetId == tee)
                : HandicapService.ResolveDefaultTeeSet(
                    teeSetsByCourse.GetValueOrDefault(round.GolfCourseId) ?? Array.Empty<TeeSet>());

            var courseHandicap = 0;
            if (entry.HandicapAtEntry is decimal index
                && teeSet is { CourseRating: decimal rating, SlopeRating: int slope })
            {
                var par = scores.Sum(s => s.Hole!.Par);
                courseHandicap = WhsCalculator.ComputeCourseHandicap(index, slope, rating, par);
            }

            entry.NetScore = ScoringCalculator.ComputeNetScore(entry.GrossScore.Value, courseHandicap);
            entry.StablefordPoints = ScoringCalculator.ComputeStablefordPoints(
                scores.Select(s => (s.Hole!.Par, s.Strokes, s.Hole!.StrokeIndex ?? WhsCalculator.RoundHoles)),
                courseHandicap);
        }

        // Competition-style ranking with shared positions (1, 2, 2, 4).
        var scored = competition.Entries.Where(e => e.GrossScore is not null);
        var ranked = (competition.ScoringFormat == ScoringFormat.Stableford
                ? scored.OrderByDescending(e => e.StablefordPoints)
                : scored.OrderBy(e => e.NetScore))
            .ToList();
        for (var i = 0; i < ranked.Count; i++)
        {
            var tiedWithPrevious = i > 0 && (competition.ScoringFormat == ScoringFormat.Stableford
                ? ranked[i].StablefordPoints == ranked[i - 1].StablefordPoints
                : ranked[i].NetScore == ranked[i - 1].NetScore);
            ranked[i].Position = tiedWithPrevious ? ranked[i - 1].Position : i + 1;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Computed results for Competition {CompetitionId}: {Ranked} ranked of {Total} entries.",
            competitionId, ranked.Count, competition.Entries.Count);

        return ranked.Concat(competition.Entries.Where(e => e.GrossScore is null)).ToList();
    }

    private static void EnsureNotTerminal(Competition competition)
    {
        if (competition.Status is CompetitionStatus.Completed or CompetitionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Competition {competition.CompetitionId} is {competition.Status} and can no longer be changed.");
        }
    }

    private static void EnsureRoundOwnership(Round round, string requestingUserId, bool isUserAdmin)
    {
        if (!isUserAdmin && round.CreatedByApplicationUserId != requestingUserId)
        {
            throw new UnauthorizedAccessException(
                $"Round {round.RoundId} does not belong to the requesting user.");
        }
    }
}
