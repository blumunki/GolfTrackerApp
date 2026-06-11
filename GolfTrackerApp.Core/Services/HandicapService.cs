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

            // Tee selection lives on RoundPlayer; scores carry a denormalised copy.
            var teeSetId = roundPlayer.TeeSetId
                ?? scores.Select(s => s.TeeSetId).FirstOrDefault(t => t.HasValue);
            if (teeSetId is null)
            {
                continue;
            }

            var teeSet = await context.TeeSets.FindAsync(teeSetId.Value);
            if (teeSet is not { CourseRating: decimal rating, SlopeRating: int slope })
            {
                continue; // no rating/slope — not qualifying
            }

            // v1 uses Hole.Par (not tee-specific par), matching the rest of the app.
            var adjustedGross = WhsCalculator.ComputeAdjustedGrossScore(
                scores.Select(s => (s.Hole!.Par, s.Strokes)));
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

            existing.TeeSetId = teeSetId.Value;
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
