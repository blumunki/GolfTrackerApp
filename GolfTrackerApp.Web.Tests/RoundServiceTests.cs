using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

/// <summary>
/// Characterization tests pinning down RoundService behaviour before Phase 2
/// adds the handicap completion hook (see ARCHITECTURE.md §12.5 Phase 4a).
/// </summary>
public sealed class RoundServiceTests : IDisposable
{
    private const string OtherUserId = "test-user-2";

    private readonly SqliteTestDbFactory _factory = new();
    private readonly RoundService _service;

    public RoundServiceTests()
    {
        _service = new RoundService(_factory, NullLogger<RoundService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    private async Task<(GolfCourse Course, Player Player)> SeedBaselineAsync()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(
            _factory, linkedUserId: TestDataBuilder.DefaultUserId);
        return (course, player);
    }

    private static Round NewRound(int courseId, string userId = TestDataBuilder.DefaultUserId) => new()
    {
        GolfCourseId = courseId,
        DatePlayed = new DateTime(2026, 6, 1),
        StartingHole = 1,
        HolesPlayed = 18,
        CreatedByApplicationUserId = userId,
    };

    // ---- AddRoundAsync validation ----

    [Fact]
    public async Task AddRound_WithoutCreator_Throws()
    {
        var (course, player) = await SeedBaselineAsync();
        var round = NewRound(course.GolfCourseId, userId: "");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddRoundAsync(round, new[] { player.PlayerId }));
    }

    [Fact]
    public async Task AddRound_UnknownCourse_Throws()
    {
        var (_, player) = await SeedBaselineAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddRoundAsync(NewRound(courseId: 9999), new[] { player.PlayerId }));
    }

    [Fact]
    public async Task AddRound_NoPlayers_Throws()
    {
        var (course, _) = await SeedBaselineAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddRoundAsync(NewRound(course.GolfCourseId), Array.Empty<int>()));
    }

    [Fact]
    public async Task AddRound_UnknownPlayer_Throws()
    {
        var (course, player) = await SeedBaselineAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddRoundAsync(NewRound(course.GolfCourseId), new[] { player.PlayerId, 9999 }));
        Assert.Contains("9999", ex.Message);
    }

    [Fact]
    public async Task AddRound_PartialTeeSelections_LeavesUnselectedPlayersNull()
    {
        var (course, player) = await SeedBaselineAsync();
        var second = await TestDataBuilder.SeedPlayerAsync(_factory, "Second", "Player");
        int teeSetId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            teeSetId = await context.TeeSets.Select(t => t.TeeSetId).SingleAsync();
        }

        await _service.AddRoundAsync(
            NewRound(course.GolfCourseId),
            new[] { player.PlayerId, second.PlayerId },
            new Dictionary<int, int?> { [player.PlayerId] = teeSetId }); // no entry for second

        await using var verify = await _factory.CreateDbContextAsync();
        var roundPlayers = await verify.RoundPlayers.ToListAsync();
        Assert.Equal(teeSetId, roundPlayers.Single(rp => rp.PlayerId == player.PlayerId).TeeSetId);
        Assert.Null(roundPlayers.Single(rp => rp.PlayerId == second.PlayerId).TeeSetId);
    }

    // ---- UpdateRoundAsync ----

    [Fact]
    public async Task UpdateRound_UnknownRound_ReturnsNull()
    {
        await SeedBaselineAsync();

        var result = await _service.UpdateRoundAsync(new Round { RoundId = 9999 });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRound_PersistsStatusTransitionToCompleted()
    {
        // Phase 2's handicap hook will key off this exact transition.
        var (course, player) = await SeedBaselineAsync();
        var round = await _service.AddRoundAsync(NewRound(course.GolfCourseId), new[] { player.PlayerId });
        Assert.Equal(RoundCompletionStatus.InProgress, round.Status);

        var updated = NewRound(course.GolfCourseId);
        updated.RoundId = round.RoundId;
        updated.Status = RoundCompletionStatus.Completed;
        updated.Notes = "windy";
        var result = await _service.UpdateRoundAsync(updated);

        Assert.NotNull(result);
        await using var verify = await _factory.CreateDbContextAsync();
        var persisted = await verify.Rounds.SingleAsync(r => r.RoundId == round.RoundId);
        Assert.Equal(RoundCompletionStatus.Completed, persisted.Status);
        Assert.Equal("windy", persisted.Notes);
    }

    [Fact]
    public async Task UpdateRound_ReplacesPlayerList_SkippingUnknownIds()
    {
        var (course, player) = await SeedBaselineAsync();
        var second = await TestDataBuilder.SeedPlayerAsync(_factory, "Second", "Player");
        var round = await _service.AddRoundAsync(NewRound(course.GolfCourseId), new[] { player.PlayerId });

        var updated = NewRound(course.GolfCourseId);
        updated.RoundId = round.RoundId;
        // Replace player with second; 9999 doesn't exist and is silently skipped.
        await _service.UpdateRoundAsync(updated, new[] { second.PlayerId, 9999 });

        await using var verify = await _factory.CreateDbContextAsync();
        var roundPlayer = await verify.RoundPlayers.SingleAsync();
        Assert.Equal(second.PlayerId, roundPlayer.PlayerId);
    }

    [Fact]
    public async Task UpdateRound_ChangingToUnknownCourse_Throws()
    {
        var (course, player) = await SeedBaselineAsync();
        var round = await _service.AddRoundAsync(NewRound(course.GolfCourseId), new[] { player.PlayerId });

        var updated = NewRound(courseId: 9999);
        updated.RoundId = round.RoundId;

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateRoundAsync(updated));
    }

    // ---- DeleteRoundAsync ----

    [Fact]
    public async Task DeleteRound_RemovesScoresAndRoundPlayers()
    {
        var (course, player) = await SeedBaselineAsync();
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId);

        var deleted = await _service.DeleteRoundAsync(round.RoundId);

        Assert.True(deleted);
        await using var verify = await _factory.CreateDbContextAsync();
        Assert.False(await verify.Rounds.AnyAsync());
        Assert.False(await verify.Scores.AnyAsync());
        Assert.False(await verify.RoundPlayers.AnyAsync());
    }

    [Fact]
    public async Task DeleteRound_UnknownRound_ReturnsFalse()
    {
        await SeedBaselineAsync();

        Assert.False(await _service.DeleteRoundAsync(9999));
    }

    // ---- Access filtering ----

    private async Task<(Round Mine, Round Theirs)> SeedTwoUsersWithRoundsAsync(GolfCourse course, Player myPlayer)
    {
        await TestDataBuilder.SeedUserAsync(_factory, OtherUserId);
        var theirPlayer = await TestDataBuilder.SeedPlayerAsync(
            _factory, "Other", "Owner", createdByUserId: OtherUserId, linkedUserId: OtherUserId);

        var mine = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, myPlayer.PlayerId);
        var theirs = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, theirPlayer.PlayerId,
            createdByUserId: OtherUserId);
        return (mine, theirs);
    }

    [Fact]
    public async Task GetAllRounds_AdminSeesEverything()
    {
        var (course, player) = await SeedBaselineAsync();
        await SeedTwoUsersWithRoundsAsync(course, player);

        var rounds = await _service.GetAllRoundsAsync(TestDataBuilder.DefaultUserId, isUserAdmin: true);

        Assert.Equal(2, rounds.Count);
    }

    [Fact]
    public async Task GetAllRounds_NonAdminSeesOnlyOwnOrParticipated()
    {
        var (course, player) = await SeedBaselineAsync();
        var (mine, _) = await SeedTwoUsersWithRoundsAsync(course, player);

        var rounds = await _service.GetAllRoundsAsync(TestDataBuilder.DefaultUserId, isUserAdmin: false);

        var round = Assert.Single(rounds);
        Assert.Equal(mine.RoundId, round.RoundId);
    }

    [Fact]
    public async Task GetRoundsForPlayer_OtherUsersPlayer_OnlyShowsRoundsRequesterIsPartOf()
    {
        var (course, player) = await SeedBaselineAsync();
        await TestDataBuilder.SeedUserAsync(_factory, OtherUserId);
        var theirPlayer = await TestDataBuilder.SeedPlayerAsync(
            _factory, "Other", "Owner", createdByUserId: OtherUserId, linkedUserId: OtherUserId);

        // Their solo round — invisible to me.
        await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, theirPlayer.PlayerId, createdByUserId: OtherUserId);
        // A round I created that they played in — visible to me.
        var shared = await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]>
            {
                [player.PlayerId] = Enumerable.Repeat(5, 18).ToArray(),
                [theirPlayer.PlayerId] = Enumerable.Repeat(5, 18).ToArray(),
            });

        var visible = await _service.GetRoundsForPlayerAsync(
            theirPlayer.PlayerId, TestDataBuilder.DefaultUserId, isUserAdmin: false);

        var round = Assert.Single(visible);
        Assert.Equal(shared.RoundId, round.RoundId);
    }

    [Fact]
    public async Task GetRecentRounds_NoLinkedPlayer_ReturnsEmpty()
    {
        var (course, player) = await SeedBaselineAsync();
        await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId);
        await TestDataBuilder.SeedUserAsync(_factory, OtherUserId); // no player linked

        var rounds = await _service.GetRecentRoundsAsync(OtherUserId, isUserAdmin: true, count: 5);

        Assert.Empty(rounds);
    }

    [Fact]
    public async Task SearchRounds_MatchesClubNameWithinAccessScope()
    {
        var (course, player) = await SeedBaselineAsync();
        await SeedTwoUsersWithRoundsAsync(course, player); // both rounds at "Test Golf Club"

        var hits = await _service.SearchRoundsAsync(
            TestDataBuilder.DefaultUserId, isUserAdmin: false, searchTerm: "Test Golf");

        Assert.Single(hits); // their round matches the term but is out of scope
        Assert.Empty(await _service.SearchRoundsAsync(
            TestDataBuilder.DefaultUserId, isUserAdmin: false, searchTerm: "Nonexistent"));
    }

    // ---- PrepareScorecardAsync ----

    [Fact]
    public async Task PrepareScorecard_WrapsAroundLastHole()
    {
        var (course, player) = await SeedBaselineAsync();

        var scorecard = await _service.PrepareScorecardAsync(
            course.GolfCourseId, startingHole: 17, holesPlayed: 4, new List<Player> { player });

        Assert.Equal(new[] { 17, 18, 1, 2 }, scorecard.PlayedHoles.Select(h => h.HoleNumber));
        Assert.Equal(4, scorecard.Scores[player.PlayerId].Count);
    }

    [Fact]
    public async Task PrepareScorecard_UsesTeeSpecificDataWhenSelected()
    {
        var (course, player) = await SeedBaselineAsync();
        var second = await TestDataBuilder.SeedPlayerAsync(_factory, "Second", "Player");

        int teeSetId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            // Make the tee data distinguishable from the hole defaults.
            teeSetId = (await context.TeeSets.SingleAsync()).TeeSetId;
            await context.HoleTees.ExecuteUpdateAsync(s => s.SetProperty(ht => ht.LengthYards, 999));
        }

        var scorecard = await _service.PrepareScorecardAsync(
            course.GolfCourseId, startingHole: 1, holesPlayed: 18,
            new List<Player> { player, second },
            new Dictionary<int, int?> { [player.PlayerId] = teeSetId }); // second: no tee

        Assert.All(scorecard.Scores[player.PlayerId], s => Assert.Equal(999, s.LengthYards));
        Assert.All(scorecard.Scores[player.PlayerId], s => Assert.Equal(teeSetId, s.TeeSetId));
        Assert.All(scorecard.Scores[second.PlayerId], s => Assert.NotEqual(999, s.LengthYards));
        Assert.All(scorecard.Scores[second.PlayerId], s => Assert.Null(s.TeeSetId));
    }

    [Fact]
    public async Task PrepareScorecard_CourseWithoutHoles_Throws()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holes: 0);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PrepareScorecardAsync(
                course.GolfCourseId, 1, 18, new List<Player> { player }));
    }
}
