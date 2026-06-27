using GolfTrackerApp.Core.Services;

namespace GolfTrackerApp.Web.Tests;

public sealed class ScoringCalculatorTests
{
    // --- Medal: net = gross − course handicap ---

    [Fact]
    public void ComputeNetScore_SubtractsCourseHandicap()
    {
        Assert.Equal(72, ScoringCalculator.ComputeNetScore(grossScore: 90, courseHandicap: 18));
        Assert.Equal(90, ScoringCalculator.ComputeNetScore(90, 0));
        Assert.Equal(92, ScoringCalculator.ComputeNetScore(90, -2)); // plus handicap adds strokes
    }

    // --- Stableford: points per hole = max(0, par − net + 2), net = gross − strokes received ---

    [Theory]
    [InlineData(4, 4, 0, 1, 2)]   // scratch net par → 2
    [InlineData(4, 3, 0, 1, 3)]   // net birdie → 3
    [InlineData(4, 5, 0, 1, 1)]   // net bogey → 1
    [InlineData(4, 6, 0, 1, 0)]   // net double bogey → 0
    [InlineData(4, 7, 0, 1, 0)]   // worse than double → still 0
    [InlineData(4, 2, 0, 1, 4)]   // net eagle → 4
    [InlineData(4, 5, 18, 1, 2)]  // CH 18 → 1 stroke on SI 1, net 4 → 2
    [InlineData(4, 5, 18, 18, 2)] // CH 18 → 1 stroke on every hole
    [InlineData(4, 5, 36, 1, 3)]  // CH 36 → 2 strokes on SI 1, net 3 → 3
    public void StablefordPointsForHole_AwardsNetPoints(int par, int gross, int courseHandicap, int strokeIndex, int expected)
    {
        Assert.Equal(expected, ScoringCalculator.StablefordPointsForHole(par, gross, courseHandicap, strokeIndex));
    }

    [Fact]
    public void ComputeStablefordPoints_SumsHoles()
    {
        // 18 par-4 holes, bogey golf (5 each), scratch → 1 point/hole = 18.
        var holes = Enumerable.Range(1, 18).Select(si => (Par: 4, Strokes: 5, StrokeIndex: si)).ToArray();
        Assert.Equal(18, ScoringCalculator.ComputeStablefordPoints(holes, courseHandicap: 0));

        // Same scores off 18 → 1 stroke/hole → net par → 2 points/hole = 36.
        Assert.Equal(36, ScoringCalculator.ComputeStablefordPoints(holes, courseHandicap: 18));
    }

    // --- Match play: compare net scores hole by hole ---

    [Fact]
    public void ComputeMatchPlay_LowerNetWinsEachHole()
    {
        // A scores 4 on every hole, B scores 5; both scratch → A wins all 18.
        var holes = Enumerable.Range(1, 18)
            .Select(si => (StrokeIndex: si, Par: 4, GrossA: 4, GrossB: 5))
            .ToArray();

        var result = ScoringCalculator.ComputeMatchPlay(holes, courseHandicapA: 0, courseHandicapB: 0);

        Assert.Equal(18, result.HolesWonA);
        Assert.Equal(0, result.HolesWonB);
        Assert.Equal(0, result.Halved);
        Assert.Equal(18, result.Margin);
    }

    [Fact]
    public void ComputeMatchPlay_HandicapStrokesCanHalveOrWinHoles()
    {
        // A gross 5, B gross 4 every hole, but A gets a stroke on every hole (CH 18) → nets level → all halved.
        var holes = Enumerable.Range(1, 18)
            .Select(si => (StrokeIndex: si, Par: 4, GrossA: 5, GrossB: 4))
            .ToArray();

        var result = ScoringCalculator.ComputeMatchPlay(holes, courseHandicapA: 18, courseHandicapB: 0);

        Assert.Equal(0, result.HolesWonA);
        Assert.Equal(0, result.HolesWonB);
        Assert.Equal(18, result.Halved);
        Assert.Equal(0, result.Margin);
        Assert.Equal("All square", result.Outcome);
    }

    [Fact]
    public void ComputeMatchPlay_ReportsLeaderAndMargin()
    {
        // B wins the first 3 holes, rest halved → B 3 up.
        var holes = Enumerable.Range(1, 18)
            .Select(si => (StrokeIndex: si, Par: 4, GrossA: si <= 3 ? 5 : 4, GrossB: 4))
            .ToArray();

        var result = ScoringCalculator.ComputeMatchPlay(holes, courseHandicapA: 0, courseHandicapB: 0);

        Assert.Equal(0, result.HolesWonA);
        Assert.Equal(3, result.HolesWonB);
        Assert.Equal(15, result.Halved);
        Assert.Equal(-3, result.Margin);
        Assert.Equal("Player B 3 up", result.Outcome);
    }
}
