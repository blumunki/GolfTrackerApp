using GolfTrackerApp.Core.Services;

namespace GolfTrackerApp.Web.Tests;

public sealed class WhsCalculatorTests
{
    // --- ComputeDifferential: (113 / slope) × (AGS − course rating), 1 dp ---

    [Fact]
    public void ComputeDifferential_StandardSlope_IsScoreMinusRating()
    {
        Assert.Equal(13.0m, WhsCalculator.ComputeDifferential(85, 72.0m, 113m));
    }

    [Fact]
    public void ComputeDifferential_NonStandardSlope_ScalesBy113OverSlope()
    {
        // (113 / 125) × (90 − 71.5) = 0.904 × 18.5 = 16.724 → 16.7
        Assert.Equal(16.7m, WhsCalculator.ComputeDifferential(90, 71.5m, 125m));
    }

    [Fact]
    public void ComputeDifferential_RoundsHalfAwayFromZero()
    {
        // (113 / 113) × (80 − 70.55) = 9.45 → 9.5
        Assert.Equal(9.5m, WhsCalculator.ComputeDifferential(80, 70.55m, 113m));
    }

    [Fact]
    public void ComputeDifferential_BelowRating_IsNegative()
    {
        Assert.Equal(-4.0m, WhsCalculator.ComputeDifferential(68, 72.0m, 113m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-55)]
    public void ComputeDifferential_NonPositiveSlope_Throws(int slope)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WhsCalculator.ComputeDifferential(85, 72.0m, slope));
    }

    // --- ComputeIndex: full WHS table over the last 20 differentials ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ComputeIndex_FewerThanThreeDifferentials_ReturnsNull(int count)
    {
        Assert.Null(WhsCalculator.ComputeIndex(ConsecutiveDifferentials(count)));
    }

    // Differentials are 10, 11, 12, … so "average of lowest k" = 10 + (k−1)/2,
    // which makes every row of the WHS table directly checkable.
    [Theory]
    [InlineData(3, 8.0)]   // lowest 1 − 2.0
    [InlineData(4, 9.0)]   // lowest 1 − 1.0
    [InlineData(5, 10.0)]  // lowest 1
    [InlineData(6, 9.5)]   // avg lowest 2 − 1.0
    [InlineData(7, 10.5)]  // avg lowest 2
    [InlineData(8, 10.5)]
    [InlineData(9, 11.0)]  // avg lowest 3
    [InlineData(10, 11.0)]
    [InlineData(11, 11.0)]
    [InlineData(12, 11.5)] // avg lowest 4
    [InlineData(13, 11.5)]
    [InlineData(14, 11.5)]
    [InlineData(15, 12.0)] // avg lowest 5
    [InlineData(16, 12.0)]
    [InlineData(17, 12.5)] // avg lowest 6
    [InlineData(18, 12.5)]
    [InlineData(19, 13.0)] // avg lowest 7
    [InlineData(20, 13.5)] // avg lowest 8
    public void ComputeIndex_AppliesWhsTablePerDifferentialCount(int count, decimal expected)
    {
        Assert.Equal(expected, WhsCalculator.ComputeIndex(ConsecutiveDifferentials(count)));
    }

    [Fact]
    public void ComputeIndex_UsesOnlyTheTwentyMostRecentDifferentials()
    {
        // Two very low differentials older than the 20-round window must not count.
        var differentials = ConsecutiveDifferentials(20).Concat(new[] { 0.0m, 0.0m });

        Assert.Equal(13.5m, WhsCalculator.ComputeIndex(differentials));
    }

    [Fact]
    public void ComputeIndex_LowestDifferentialsCountRegardlessOfRecency()
    {
        // 7 differentials → average of lowest 2, wherever they sit in the window.
        var differentials = new[] { 15.0m, 14.0m, 10.2m, 13.0m, 12.0m, 11.0m, 10.3m };

        // (10.2 + 10.3) / 2 = 10.25 → 10.3 (half away from zero)
        Assert.Equal(10.3m, WhsCalculator.ComputeIndex(differentials));
    }

    [Fact]
    public void ComputeIndex_IsCappedAtMaximum54()
    {
        // Lowest 57.0 − 2.0 = 55.0 → capped at 54.0
        Assert.Equal(54.0m, WhsCalculator.ComputeIndex(new[] { 57.0m, 58.0m, 59.0m }));
    }

    [Fact]
    public void ComputeIndex_PlusHandicap_IsNegative()
    {
        Assert.Equal(-1.0m, WhsCalculator.ComputeIndex(new[] { 1.0m, 2.0m, 3.0m }));
    }

    // --- ComputeAdjustedGrossScore: per-hole cap at par + 5 (v1 rule) ---

    [Fact]
    public void ComputeAdjustedGrossScore_NoHoleAboveCap_SumsStrokes()
    {
        var holes = Enumerable.Repeat((Par: 4, Strokes: 5), 18);

        Assert.Equal(90, WhsCalculator.ComputeAdjustedGrossScore(holes));
    }

    [Fact]
    public void ComputeAdjustedGrossScore_CapsBlowUpHolesAtParPlusFive()
    {
        var holes = new[] { (Par: 4, Strokes: 12), (Par: 5, Strokes: 4) };

        Assert.Equal(9 + 4, WhsCalculator.ComputeAdjustedGrossScore(holes));
    }

    [Fact]
    public void ComputeAdjustedGrossScore_ScoreExactlyAtCap_IsUnchanged()
    {
        var holes = new[] { (Par: 3, Strokes: 8) };

        Assert.Equal(8, WhsCalculator.ComputeAdjustedGrossScore(holes));
    }

    /// <summary>Differentials 10.0, 11.0, 12.0, … (most recent first).</summary>
    private static IEnumerable<decimal> ConsecutiveDifferentials(int count) =>
        Enumerable.Range(10, count).Select(d => (decimal)d);
}
