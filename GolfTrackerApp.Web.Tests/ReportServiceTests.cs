using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class ReportServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly ReportService _service;

    /// <summary>Mixed-par layout: 4 par 3s, 10 par 4s, 4 par 5s — course par 72.</summary>
    private static readonly int[] MixedPars =
        { 4, 3, 4, 5, 4, 4, 3, 4, 4, 4, 3, 4, 5, 4, 5, 3, 4, 5 };

    /// <summary>
    /// Stroke line against <see cref="MixedPars"/> giving exactly:
    /// 1 eagle, 2 birdies, 5 pars, 5 bogeys, 2 doubles, 3 triple-or-worse.
    /// Total 87 (+15 vs par 72).
    /// </summary>
    private static readonly int[] CraftedStrokes =
        { 3, 3, 4, 3, 3, 5, 4, 4, 6, 4, 6, 5, 7, 4, 6, 7, 5, 8 };

    private static int[] Uniform(int strokes, int holes = 18) =>
        Enumerable.Repeat(strokes, holes).ToArray();

    public ReportServiceTests()
    {
        _service = new ReportService(_factory, NullLogger<ReportService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    private async Task<(GolfCourse Course, Player Player)> SeedUserCoursePlayerAsync(int[]? holePars = null)
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holePars: holePars ?? MixedPars);
        var player = await TestDataBuilder.SeedPlayerAsync(
            _factory, linkedUserId: TestDataBuilder.DefaultUserId);
        return (course, player);
    }

    // ---- Scoring distribution ----

    [Fact]
    public async Task ScoringDistribution_CountsEveryCategory()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes });

        var dist = await _service.GetScoringDistributionAsync(
            player.PlayerId, null, null, null, null, null);

        Assert.Equal(1, dist.EagleCount);
        Assert.Equal(2, dist.BirdieCount);
        Assert.Equal(5, dist.ParCount);
        Assert.Equal(5, dist.BogeyCount);
        Assert.Equal(2, dist.DoubleBogeyCount);
        Assert.Equal(3, dist.TripleBogeyOrWorseCount);
        Assert.Equal(18, dist.TotalHoles);
    }

    [Fact]
    public async Task ScoringDistribution_ExcludesInProgressRounds()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes });
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(2) },
            status: RoundCompletionStatus.InProgress);

        var dist = await _service.GetScoringDistributionAsync(
            player.PlayerId, null, null, null, null, null);

        Assert.Equal(18, dist.TotalHoles);
        Assert.Equal(1, dist.EagleCount);
    }

    // ---- Performance by par ----

    [Fact]
    public async Task PerformanceByPar_AveragesPerParType()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        var bogeyGolf = MixedPars.Select(p => p + 1).ToArray();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = bogeyGolf });

        var perf = await _service.GetPerformanceByParAsync(
            player.PlayerId, null, null, null, null, null);

        Assert.True(perf.HasValidData);
        Assert.Equal(4, perf.Par3Count);
        Assert.Equal(10, perf.Par4Count);
        Assert.Equal(4, perf.Par5Count);
        Assert.Equal(4.0, perf.Par3Average);
        Assert.Equal(5.0, perf.Par4Average);
        Assert.Equal(6.0, perf.Par5Average);
        Assert.Equal(1.0, perf.Par4RelativeToPar);
    }

    [Fact]
    public async Task PerformanceByPar_NoRounds_ReturnsEmptyResult()
    {
        var (_, player) = await SeedUserCoursePlayerAsync();

        var perf = await _service.GetPerformanceByParAsync(
            player.PlayerId, null, null, null, null, null);

        Assert.False(perf.HasValidData);
    }

    // ---- Player performance ----

    [Fact]
    public async Task PlayerPerformance_OrdersByDateAndComputesTotals()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(5) },
            datePlayed: new DateTime(2026, 5, 20));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes },
            datePlayed: new DateTime(2026, 5, 1));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(4) },
            datePlayed: new DateTime(2026, 5, 25),
            status: RoundCompletionStatus.InProgress);

        var points = await _service.GetPlayerPerformanceAsync(
            player.PlayerId, null, null, null, null, null);

        Assert.Equal(2, points.Count); // in-progress round excluded
        Assert.Equal(new DateTime(2026, 5, 1), points[0].Date);
        Assert.Equal(87, points[0].TotalScore);
        Assert.Equal(72, points[0].TotalPar);
        Assert.Equal(15, points[0].ScoreVsPar);
        Assert.Equal(90, points[1].TotalScore);
        Assert.Contains("Test Golf Club", points[0].CourseName);
    }

    [Fact]
    public async Task PlayerPerformance_FiltersByCourse()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        var otherCourse = await TestDataBuilder.SeedCourseAsync(
            _factory, clubName: "Other Club", courseName: "Other Course", holePars: MixedPars);
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(5) });
        await TestDataBuilder.SeedRoundAsync(_factory, otherCourse.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(4) });

        var points = await _service.GetPlayerPerformanceAsync(
            player.PlayerId, otherCourse.GolfCourseId, null, null, null, null);

        var point = Assert.Single(points);
        Assert.Equal(72, point.TotalScore);
        Assert.Contains("Other Club", point.CourseName);
    }

    // ---- Dashboard stats ----

    [Fact]
    public async Task DashboardStats_ComputesAggregates()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes }, // 87 (+15)
            datePlayed: new DateTime(2026, 5, 1));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = MixedPars.Select(p => p + 1).ToArray() }, // 90 (+18)
            datePlayed: new DateTime(2026, 5, 10));

        var stats = await _service.GetDashboardStatsAsync(TestDataBuilder.DefaultUserId);

        Assert.Equal(2, stats.TotalRounds);
        Assert.Equal(87, stats.BestScore);
        Assert.Equal(new DateTime(2026, 5, 1), stats.BestScoreDate);
        Assert.Equal(88.5, stats.AverageScore);
        Assert.Equal(16.5, stats.AverageToPar);
        Assert.Equal(15, stats.LowestToPar);
        Assert.Equal(1, stats.UniqueCoursesPlayed);
        Assert.Equal(1, stats.UniqueClubsVisited);
        Assert.Equal(2, stats.EighteenHoleRounds);
        Assert.Equal(0, stats.NineHoleRounds);
        Assert.Equal(2, stats.FavoriteCourseRounds);
        Assert.Equal(new DateTime(2026, 5, 10), stats.LastRoundDate);
    }

    [Fact]
    public async Task DashboardStats_NoLinkedPlayer_ReturnsEmptyStats()
    {
        await TestDataBuilder.SeedUserAsync(_factory);

        var stats = await _service.GetDashboardStatsAsync(TestDataBuilder.DefaultUserId);

        Assert.Equal(0, stats.TotalRounds);
        Assert.Null(stats.BestScore);
    }

    [Fact]
    public async Task DashboardStats_DetectsImprovingStreak()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // uniform par 4 (72)
        var player = await TestDataBuilder.SeedPlayerAsync(
            _factory, linkedUserId: TestDataBuilder.DefaultUserId);

        // One bad early round, then five better ones.
        await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId,
            datePlayed: new DateTime(2026, 1, 1), strokesPerHole: 6); // 108
        for (var i = 0; i < 5; i++)
        {
            await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId,
                datePlayed: new DateTime(2026, 2, 1).AddDays(i), strokesPerHole: 5); // 90
        }

        var stats = await _service.GetDashboardStatsAsync(TestDataBuilder.DefaultUserId);

        Assert.True(stats.IsImprovingStreak);
        Assert.Equal(5, stats.CurrentStreak);
    }

    // ---- Quick stats ----

    [Fact]
    public async Task PlayerQuickStats_ComputesBestAverageAndLastPlayed()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes }, // 87
            datePlayed: new DateTime(2026, 4, 1));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = Uniform(5) }, // 90
            datePlayed: new DateTime(2026, 4, 8));

        var stats = await _service.GetPlayerQuickStatsAsync(player.PlayerId);

        Assert.Equal(2, stats.RoundCount);
        Assert.Equal(87, stats.BestScore);
        Assert.Equal(88.5, stats.AverageScore);
        Assert.Equal(new DateTime(2026, 4, 8), stats.LastPlayed);
        Assert.Equal(16.5, stats.AverageVsPar);
    }

    // ---- Playing partners ----

    [Fact]
    public async Task PlayingPartnerSummary_TracksHeadToHeadRecord()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        var partner = await TestDataBuilder.SeedPlayerAsync(_factory, "Paula", "Partner");

        // Round 1: user wins (87 vs 90). Round 2: tie (90 vs 90).
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]>
            {
                [player.PlayerId] = CraftedStrokes,
                [partner.PlayerId] = Uniform(5),
            },
            datePlayed: new DateTime(2026, 3, 1));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]>
            {
                [player.PlayerId] = Uniform(5),
                [partner.PlayerId] = Uniform(5),
            },
            datePlayed: new DateTime(2026, 3, 8));

        var summaries = await _service.GetPlayingPartnerSummaryAsync(TestDataBuilder.DefaultUserId, 5);

        var summary = Assert.Single(summaries);
        Assert.Equal("Paula Partner", summary.PartnerName);
        Assert.Equal(1, summary.UserWins);
        Assert.Equal(0, summary.PartnerWins);
        Assert.Equal(1, summary.Ties);
        Assert.Equal(new DateTime(2026, 3, 8), summary.LastPlayedDate);
    }

    // ---- Course history ----

    [Fact]
    public async Task CourseHistory_ReportsBestAndMostRecentSeparately()
    {
        var (course, player) = await SeedUserCoursePlayerAsync();
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = CraftedStrokes }, // 87, older
            datePlayed: new DateTime(2026, 2, 1));
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [player.PlayerId] = MixedPars.Select(p => p + 1).ToArray() }, // 90, recent
            datePlayed: new DateTime(2026, 2, 15));

        var history = await _service.GetCourseHistoryAsync(TestDataBuilder.DefaultUserId);

        var item = Assert.Single(history);
        Assert.Equal(2, item.TimesPlayed);
        Assert.Equal(90, item.MostRecentScore);
        Assert.Equal(18, item.MostRecentToPar);
        Assert.Equal(87, item.BestScore);
        Assert.Equal(15, item.BestToPar);
        Assert.Equal(new DateTime(2026, 2, 15), item.LastPlayedDate);
    }

    // ---- Player comparison ----

    [Fact]
    public async Task PlayerComparison_LimitsToSharedRoundsAndScoresHeadToHead()
    {
        var (course, playerA) = await SeedUserCoursePlayerAsync();
        var playerB = await TestDataBuilder.SeedPlayerAsync(_factory, "Bobby", "Better");

        // Shared round: B beats A (88 vs 90).
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]>
            {
                [playerA.PlayerId] = Uniform(5),    // 90
                [playerB.PlayerId] = CraftedStrokes, // 87
            },
            datePlayed: new DateTime(2026, 1, 10));
        // Solo round for A — must not appear in the comparison.
        await TestDataBuilder.SeedRoundAsync(_factory, course.GolfCourseId,
            new Dictionary<int, int[]> { [playerA.PlayerId] = Uniform(4) },
            datePlayed: new DateTime(2026, 1, 20));

        var result = await _service.GetPlayerComparisonAsync(
            playerA.PlayerId, new List<int> { playerB.PlayerId }, null, null, null, null, null);

        var seriesA = result.PlayerSeries.Single(s => s.PlayerId == playerA.PlayerId);
        Assert.Single(seriesA.DataPoints); // solo round excluded

        var summaryB = result.Summaries.Single(s => s.PlayerId == playerB.PlayerId);
        Assert.Equal(1, summaryB.SharedRounds);
        Assert.Equal(1, summaryB.Wins);
        Assert.Equal(0, summaryB.Losses);
        Assert.Equal(0, summaryB.Ties);
    }
}
