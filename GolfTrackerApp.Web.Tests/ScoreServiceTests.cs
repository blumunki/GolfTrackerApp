using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class ScoreServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly ScoreService _service;

    public ScoreServiceTests()
    {
        _service = new ScoreService(_factory, NullLogger<ScoreService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    private async Task<(Round Round, Player Player, List<Hole> Holes)> SeedInProgressRoundAsync()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holes: 9);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);

        await using var context = await _factory.CreateDbContextAsync();
        var round = new Round
        {
            GolfCourseId = course.GolfCourseId,
            DatePlayed = new DateTime(2026, 6, 1),
            StartingHole = 1,
            HolesPlayed = 9,
            Status = RoundCompletionStatus.InProgress,
            CreatedByApplicationUserId = TestDataBuilder.DefaultUserId,
        };
        round.RoundPlayers.Add(new RoundPlayer { PlayerId = player.PlayerId });
        context.Rounds.Add(round);
        await context.SaveChangesAsync();

        var holes = await context.Holes
            .Where(h => h.GolfCourseId == course.GolfCourseId)
            .OrderBy(h => h.HoleNumber)
            .ToListAsync();
        return (round, player, holes);
    }

    private static Dictionary<int, List<HoleScoreEntryModel>> ScorecardFor(
        int playerId, IEnumerable<Hole> holes, Func<Hole, HoleScoreEntryModel> entry) =>
        new() { [playerId] = holes.Select(entry).ToList() };

    // ---- SaveScorecardAsync ----

    [Fact]
    public async Task SaveScorecard_PersistsOnlyEnteredScores()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        var scorecard = ScorecardFor(player.PlayerId, holes, h => new HoleScoreEntryModel
        {
            HoleId = h.HoleId,
            // Leave holes 8 and 9 unentered
            Strokes = h.HoleNumber <= 7 ? 5 : null,
            Putts = h.HoleNumber <= 7 ? 2 : null,
            FairwayHit = h.HoleNumber == 1,
        });

        await _service.SaveScorecardAsync(round.RoundId, scorecard);

        await using var verify = await _factory.CreateDbContextAsync();
        var scores = await verify.Scores.OrderBy(s => s.HoleId).ToListAsync();
        Assert.Equal(7, scores.Count);
        Assert.All(scores, s => Assert.Equal(5, s.Strokes));
        Assert.All(scores, s => Assert.Equal(2, s.Putts));
        Assert.True(scores.First().FairwayHit);
    }

    [Fact]
    public async Task SaveScorecard_ReplacesExistingScoresOnEdit()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        await _service.SaveScorecardAsync(round.RoundId, ScorecardFor(
            player.PlayerId, holes, h => new HoleScoreEntryModel { HoleId = h.HoleId, Strokes = 6 }));

        await _service.SaveScorecardAsync(round.RoundId, ScorecardFor(
            player.PlayerId, holes, h => new HoleScoreEntryModel { HoleId = h.HoleId, Strokes = 4 }));

        await using var verify = await _factory.CreateDbContextAsync();
        var scores = await verify.Scores.ToListAsync();
        Assert.Equal(9, scores.Count); // no duplicates from the first save
        Assert.All(scores, s => Assert.Equal(4, s.Strokes));
    }

    [Fact]
    public async Task SaveScorecard_MarksRoundCompleted()
    {
        // NOTE: this is a second round-completion path that bypasses
        // RoundService.UpdateRoundAsync — the Phase 2 handicap hook must
        // cover it too (WORKLOG item 2-3).
        var (round, player, holes) = await SeedInProgressRoundAsync();

        await _service.SaveScorecardAsync(round.RoundId, ScorecardFor(
            player.PlayerId, holes, h => new HoleScoreEntryModel { HoleId = h.HoleId, Strokes = 5 }));

        await using var verify = await _factory.CreateDbContextAsync();
        var persisted = await verify.Rounds.SingleAsync(r => r.RoundId == round.RoundId);
        Assert.Equal(RoundCompletionStatus.Completed, persisted.Status);
    }

    [Fact]
    public async Task SaveScorecard_PersistsTeeSetId()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        int teeSetId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            teeSetId = await context.TeeSets.Select(t => t.TeeSetId).SingleAsync();
        }

        await _service.SaveScorecardAsync(round.RoundId, ScorecardFor(
            player.PlayerId, holes,
            h => new HoleScoreEntryModel { HoleId = h.HoleId, Strokes = 5, TeeSetId = teeSetId }));

        await using var verify = await _factory.CreateDbContextAsync();
        Assert.All(await verify.Scores.ToListAsync(), s => Assert.Equal(teeSetId, s.TeeSetId));
    }

    // ---- Score CRUD ----

    [Fact]
    public async Task UpdateScore_PersistsNewValues()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        var score = await _service.AddScoreAsync(new Score
        {
            RoundId = round.RoundId,
            PlayerId = player.PlayerId,
            HoleId = holes[0].HoleId,
            Strokes = 6,
        });

        score.Strokes = 4;
        score.Putts = 1;
        var updated = await _service.UpdateScoreAsync(score);

        Assert.NotNull(updated);
        await using var verify = await _factory.CreateDbContextAsync();
        var persisted = await verify.Scores.SingleAsync();
        Assert.Equal(4, persisted.Strokes);
        Assert.Equal(1, persisted.Putts);
    }

    [Fact]
    public async Task UpdateScore_UnknownScore_ReturnsNull()
    {
        await SeedInProgressRoundAsync();

        Assert.Null(await _service.UpdateScoreAsync(new Score { ScoreId = 9999 }));
    }

    [Fact]
    public async Task DeleteScore_RemovesRow()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        var score = await _service.AddScoreAsync(new Score
        {
            RoundId = round.RoundId,
            PlayerId = player.PlayerId,
            HoleId = holes[0].HoleId,
            Strokes = 5,
        });

        Assert.True(await _service.DeleteScoreAsync(score.ScoreId));
        Assert.False(await _service.DeleteScoreAsync(score.ScoreId));
    }

    [Fact]
    public async Task GetScoresForPlayerInRound_OrdersByHoleNumber()
    {
        var (round, player, holes) = await SeedInProgressRoundAsync();
        // Insert in reverse hole order to prove the query sorts.
        foreach (var hole in holes.OrderByDescending(h => h.HoleNumber))
        {
            await _service.AddScoreAsync(new Score
            {
                RoundId = round.RoundId,
                PlayerId = player.PlayerId,
                HoleId = hole.HoleId,
                Strokes = 5,
            });
        }

        var scores = await _service.GetScoresForPlayerInRoundAsync(round.RoundId, player.PlayerId);

        Assert.Equal(Enumerable.Range(1, 9), scores.Select(s => s.Hole!.HoleNumber));
    }
}
