using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

/// <summary>
/// Integration tests for the round-completion handicap pipeline: both completion
/// paths (RoundService.UpdateRoundAsync and ScoreService.SaveScorecardAsync) must
/// produce scoring differentials, and the personal WHS index must appear once
/// three qualifying differentials exist.
/// Seeded course: 18 holes, par 4, course rating 70.0, slope 120 — so bogey golf
/// (90) gives differential (113/120) × 20 = 18.8.
/// </summary>
public sealed class HandicapServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly HandicapService _handicapService;
    private readonly RoundService _roundService;
    private readonly ScoreService _scoreService;

    public HandicapServiceTests()
    {
        _handicapService = new HandicapService(_factory, NullLogger<HandicapService>.Instance);
        _roundService = new RoundService(_factory, NullLogger<RoundService>.Instance, _handicapService);
        _scoreService = new ScoreService(_factory, NullLogger<ScoreService>.Instance, _handicapService);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UpdateRound_TransitionToCompleted_CreatesDifferential()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var round = await TestDataBuilder.SeedRoundAsync(
            _factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Strokes(5) },
            status: RoundCompletionStatus.InProgress,
            teeSetId: teeSetId);

        var update = (await _roundService.GetRoundByIdAsync(round.RoundId))!;
        update.Status = RoundCompletionStatus.Completed;
        await _roundService.UpdateRoundAsync(update);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(90, differential.AdjustedGrossScore);
        Assert.Equal(18.8m, differential.Differential);
        Assert.Equal(teeSetId, differential.TeeSetId);
    }

    [Fact]
    public async Task SaveScorecard_CompletesRoundAndCreatesDifferential()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var round = await TestDataBuilder.SeedRoundAsync(
            _factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Strokes(5) },
            status: RoundCompletionStatus.InProgress,
            teeSetId: teeSetId);

        // Re-save the scorecard at level par via the second completion path.
        await using var context = await _factory.CreateDbContextAsync();
        var holeIds = await context.Holes
            .Where(h => h.GolfCourseId == course.GolfCourseId)
            .OrderBy(h => h.HoleNumber)
            .Select(h => h.HoleId)
            .ToListAsync();
        var scorecard = new Dictionary<int, List<HoleScoreEntryModel>>
        {
            [player.PlayerId] = holeIds
                .Select(id => new HoleScoreEntryModel { HoleId = id, Strokes = 4, TeeSetId = teeSetId })
                .ToList(),
        };

        await _scoreService.SaveScorecardAsync(round.RoundId, scorecard);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(72, differential.AdjustedGrossScore);
        Assert.Equal(1.9m, differential.Differential); // (113/120) × 2 = 1.883 → 1.9
        var persisted = await context.Rounds.SingleAsync(r => r.RoundId == round.RoundId);
        Assert.Equal(RoundCompletionStatus.Completed, persisted.Status);
    }

    [Fact]
    public async Task ThreeQualifyingRounds_CreateHandicapRecordWithLowestMinusTwo()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, strokesPerHole: 5, daysAgo: 3);
        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, strokesPerHole: 6, daysAgo: 2);
        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, strokesPerHole: 7, daysAgo: 1);

        await using var context = await _factory.CreateDbContextAsync();
        var record = await context.HandicapRecords.SingleAsync();
        Assert.Equal(HandicapSource.Personal, record.Source);
        Assert.Equal(16.8m, record.HandicapIndex); // lowest (18.8) − 2.0
        Assert.False(record.IsManualEntry);
        Assert.NotNull(record.CalculationDetails);

        // Only the lowest differential counts at 3 differentials.
        var flags = await context.ScoringDifferentials
            .OrderBy(d => d.Differential)
            .Select(d => d.IsUsedInCalculation)
            .ToListAsync();
        Assert.Equal(new[] { true, false, false }, flags);
    }

    [Fact]
    public async Task Recalculation_IsIdempotent()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var lastRoundId = 0;
        lastRoundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 5, daysAgo: 3);
        lastRoundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 6, daysAgo: 2);
        lastRoundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 7, daysAgo: 1);

        await _handicapService.OnRoundCompletedAsync(lastRoundId);
        await _handicapService.OnRoundCompletedAsync(lastRoundId);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(3, await context.ScoringDifferentials.CountAsync());
        Assert.Equal(1, await context.HandicapRecords.CountAsync()); // index unchanged → no new record
    }

    [Fact]
    public async Task NineHoleRound_DoesNotQualify()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holes: 9);
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);

        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 5, daysAgo: 1);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(0, await context.ScoringDifferentials.CountAsync());
    }

    [Fact]
    public async Task TeeSetWithoutRating_DoesNotQualify()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, courseRating: null, slopeRating: null);
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);

        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 5, daysAgo: 1);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(0, await context.ScoringDifferentials.CountAsync());
    }

    [Fact]
    public async Task BlowUpHole_IsCappedAtParPlusFive()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var strokes = Strokes(5);
        strokes[0] = 15; // par 4 → counts as 9

        var round = await TestDataBuilder.SeedRoundAsync(
            _factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = strokes },
            teeSetId: teeSetId);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(94, differential.AdjustedGrossScore); // 17×5 + 9
        Assert.Equal(22.6m, differential.Differential);    // (113/120) × 24
    }

    [Fact]
    public async Task IndexChange_UpdatesDisplayHandicap_WhenPrimarySourceIsPersonal()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        await using (var context = await _factory.CreateDbContextAsync())
        {
            (await context.Players.FindAsync(player.PlayerId))!.PrimaryHandicapSource = HandicapSource.Personal;
            await context.SaveChangesAsync();
        }

        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 5, daysAgo: 3);
        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 6, daysAgo: 2);
        await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 7, daysAgo: 1);

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var persisted = await context.Players.SingleAsync(p => p.PlayerId == player.PlayerId);
            Assert.Equal(16.8, persisted.Handicap);
        }
    }

    [Fact]
    public async Task UpdateRoundScores_RecalculatesDifferential()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var roundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, strokesPerHole: 5, daysAgo: 1);
        Assert.Equal(18.8m, (await SingleDifferentialAsync()).Differential); // bogey golf

        // Edit every score down to level par via the mobile score-edit path.
        List<ScoreUpdateDto> updates;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            updates = await context.Scores
                .Where(s => s.RoundId == roundId)
                .Select(s => new ScoreUpdateDto { ScoreId = s.ScoreId, Strokes = 4 })
                .ToListAsync();
        }

        var updated = await _scoreService.UpdateRoundScoresAsync(roundId, updates);

        Assert.Equal(18, updated);
        var differential = await SingleDifferentialAsync();
        Assert.Equal(72, differential.AdjustedGrossScore);
        Assert.Equal(1.9m, differential.Differential); // recalculated, not stale
    }

    [Fact]
    public async Task UpdateRoundScores_IgnoresScoreIdsFromOtherRounds()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var targetRoundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 5, daysAgo: 2);
        var otherRoundId = await CompleteRoundAsync(course.GolfCourseId, player.PlayerId, teeSetId, 6, daysAgo: 1);

        int foreignScoreId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            foreignScoreId = await context.Scores
                .Where(s => s.RoundId == otherRoundId)
                .Select(s => s.ScoreId)
                .FirstAsync();
        }

        var updated = await _scoreService.UpdateRoundScoresAsync(
            targetRoundId, new[] { new ScoreUpdateDto { ScoreId = foreignScoreId, Strokes = 1 } });

        Assert.Equal(0, updated);
        await using (var context = await _factory.CreateDbContextAsync())
        {
            var foreignScore = await context.Scores.SingleAsync(s => s.ScoreId == foreignScoreId);
            Assert.Equal(6, foreignScore.Strokes); // untouched
        }
    }

    [Fact]
    public async Task UpdateRoundScores_OnInProgressRound_DoesNotCreateDifferential()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var round = await TestDataBuilder.SeedRoundAsync(
            _factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Strokes(5) },
            status: RoundCompletionStatus.InProgress,
            teeSetId: teeSetId);

        List<ScoreUpdateDto> updates;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            updates = await context.Scores
                .Where(s => s.RoundId == round.RoundId)
                .Select(s => new ScoreUpdateDto { ScoreId = s.ScoreId, Strokes = 4 })
                .ToListAsync();
        }
        var updated = await _scoreService.UpdateRoundScoresAsync(round.RoundId, updates);

        Assert.Equal(18, updated);
        await using (var verify = await _factory.CreateDbContextAsync())
        {
            Assert.Equal(0, await verify.ScoringDifferentials.CountAsync()); // edit must not complete the round
            var persisted = await verify.Rounds.SingleAsync(r => r.RoundId == round.RoundId);
            Assert.Equal(RoundCompletionStatus.InProgress, persisted.Status);
        }
    }

    [Fact]
    public async Task HistoricalRound_WithNullTee_QualifiesViaCourseDefault()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // single "Yellow" tee, CR 70.0 / slope 120

        // Historical round: no tee selection at all (the realistic backfill case).
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, strokesPerHole: 5, teeSetId: null);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(18.8m, differential.Differential); // bogey golf via the default Yellow tee
    }

    [Fact]
    public async Task NullTee_PrefersYellowOverOtherTees()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // Yellow: CR 70.0 / slope 120

        // Add a White tee with a very different rating; the null-tee default must still be Yellow.
        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.TeeSets.Add(new TeeSet
            {
                GolfCourseId = course.GolfCourseId,
                Name = "White",
                CourseRating = 60.0m,
                SlopeRating = 100,
                SortOrder = 0, // lower sort order than Yellow — name preference must win
            });
            await context.SaveChangesAsync();
        }

        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, strokesPerHole: 5, teeSetId: null);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(70.0m, differential.CourseRating); // Yellow, not White's 60.0
        Assert.Equal(18.8m, differential.Differential);
    }

    [Fact]
    public async Task NullTee_NoYellow_UsesLowestSortOrderTee()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        // Course's only/seeded tee is "White" (sort order 1) — no Yellow exists.
        var course = await TestDataBuilder.SeedCourseAsync(_factory, teeName: "White");

        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, strokesPerHole: 5, teeSetId: null);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);

        var differential = await SingleDifferentialAsync();
        Assert.Equal(18.8m, differential.Differential); // qualifies via White as the lowest-sort default
    }

    [Fact]
    public async Task NullTee_DefaultTeeHasNoRating_DoesNotQualify()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, courseRating: null, slopeRating: null);

        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, strokesPerHole: 5, teeSetId: null);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(0, await context.ScoringDifferentials.CountAsync());
    }

    [Fact]
    public async Task Backfill_ReportsQualifiedOfProcessedAndBuildsHistory()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        var nineHoleCourse = await TestDataBuilder.SeedCourseAsync(
            _factory, clubName: "Nine Hole Club", courseName: "Short", holes: 9);

        // Three qualifying rounds (diffs 18.8, 35.8, 52.7), one level-par round
        // (diff 1.9), and one nine-hole round that cannot qualify. No hooks fired —
        // this is pre-existing history.
        foreach (var (strokes, daysAgo) in new[] { (5, 5), (6, 4), (7, 3), (4, 2) })
        {
            await TestDataBuilder.SeedCompletedRoundAsync(
                _factory, course.GolfCourseId, player.PlayerId,
                datePlayed: DateTime.UtcNow.Date.AddDays(-daysAgo),
                strokesPerHole: strokes, teeSetId: teeSetId);
        }
        await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, nineHoleCourse.GolfCourseId, player.PlayerId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-1));

        var result = await _handicapService.BackfillPersonalHandicapsAsync();

        Assert.Equal(5, result.RoundsProcessed);
        Assert.Equal(4, result.RoundsQualified);
        Assert.Equal(4, result.DifferentialsWritten);
        Assert.Equal(1, result.PlayersWithIndex);
        // History evolves in playing order: index 16.8 after the 3rd round
        // (lowest 18.8 − 2.0), then 0.9 after the 4th (lowest 1.9 − 1.0).
        Assert.Equal(2, result.HandicapRecordsCreated);

        await using var context = await _factory.CreateDbContextAsync();
        var indexes = await context.HandicapRecords
            .OrderBy(h => h.EffectiveDate)
            .Select(h => h.HandicapIndex)
            .ToListAsync();
        Assert.Equal(new[] { 16.8m, 0.9m }, indexes);
    }

    [Fact]
    public async Task Backfill_IsIdempotentAcrossRuns()
    {
        var (player, course, teeSetId) = await SeedBaseAsync();
        foreach (var (strokes, daysAgo) in new[] { (5, 3), (6, 2), (7, 1) })
        {
            await TestDataBuilder.SeedCompletedRoundAsync(
                _factory, course.GolfCourseId, player.PlayerId,
                datePlayed: DateTime.UtcNow.Date.AddDays(-daysAgo),
                strokesPerHole: strokes, teeSetId: teeSetId);
        }

        var first = await _handicapService.BackfillPersonalHandicapsAsync();
        var second = await _handicapService.BackfillPersonalHandicapsAsync();

        Assert.Equal(1, first.HandicapRecordsCreated);
        Assert.Equal(0, second.HandicapRecordsCreated); // indexes unchanged
        Assert.Equal(first.RoundsQualified, second.RoundsQualified);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(3, await context.ScoringDifferentials.CountAsync());
        Assert.Equal(1, await context.HandicapRecords.CountAsync());
    }

    private async Task<(Player Player, GolfCourse Course, int TeeSetId)> SeedBaseAsync()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // 18 holes, CR 70.0, slope 120
        return (player, course, await TeeSetIdAsync(course.GolfCourseId));
    }

    /// <summary>Seeds a completed round and fires the hook the way both completion paths do.</summary>
    private async Task<int> CompleteRoundAsync(int courseId, int playerId, int teeSetId, int strokesPerHole, int daysAgo)
    {
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, courseId, playerId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-daysAgo),
            strokesPerHole: strokesPerHole,
            teeSetId: teeSetId);
        await _handicapService.OnRoundCompletedAsync(round.RoundId);
        return round.RoundId;
    }

    private async Task<int> TeeSetIdAsync(int courseId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.TeeSets
            .Where(ts => ts.GolfCourseId == courseId)
            .Select(ts => ts.TeeSetId)
            .SingleAsync();
    }

    private async Task<ScoringDifferential> SingleDifferentialAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.ScoringDifferentials.SingleAsync();
    }

    private static int[] Strokes(int perHole) => Enumerable.Repeat(perHole, 18).ToArray();
}
