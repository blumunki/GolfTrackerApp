namespace GolfTrackerApp.Core.Services;

/// <summary>Result of an 18-hole match-play comparison between two players.</summary>
public record MatchPlayResult(int HolesWonA, int HolesWonB, int Halved)
{
    /// <summary>Holes A leads by (negative = B leads).</summary>
    public int Margin => HolesWonA - HolesWonB;

    /// <summary>Plain-English standing (final margin; closeout "3&amp;2" notation is a future refinement).</summary>
    public string Outcome => Margin switch
    {
        0 => "All square",
        > 0 => $"Player A {Margin} up",
        _ => $"Player B {-Margin} up",
    };
}

/// <summary>
/// Pure competition scoring math — no data access (WhsCalculator precedent). Course handicaps
/// are computed by the caller (via <see cref="WhsCalculator.ComputeCourseHandicap"/>) and passed
/// in. v1 covers Medal, Stableford and Match Play; strokes received reuse the WHS allocation.
/// </summary>
public static class ScoringCalculator
{
    /// <summary>Medal (stroke play) net score: gross − course handicap.</summary>
    public static int ComputeNetScore(int grossScore, int courseHandicap) => grossScore - courseHandicap;

    /// <summary>
    /// Stableford points for one hole: <c>max(0, par − net + 2)</c>, where net is gross minus the
    /// strokes the player receives on that hole. Net par = 2, net birdie = 3, net bogey = 1,
    /// net double bogey (or worse) = 0.
    /// </summary>
    public static int StablefordPointsForHole(int par, int grossStrokes, int courseHandicap, int strokeIndex)
    {
        var net = grossStrokes - WhsCalculator.StrokesReceivedOnHole(courseHandicap, strokeIndex);
        return Math.Max(0, par - net + 2);
    }

    /// <summary>Total Stableford points across the holes played.</summary>
    public static int ComputeStablefordPoints(
        IEnumerable<(int Par, int Strokes, int StrokeIndex)> holes, int courseHandicap) =>
        holes.Sum(h => StablefordPointsForHole(h.Par, h.Strokes, courseHandicap, h.StrokeIndex));

    /// <summary>
    /// Match play between two players over the same holes. Each hole is won by the lower net
    /// score (gross minus that player's strokes received on the hole); equal nets halve the hole.
    /// </summary>
    public static MatchPlayResult ComputeMatchPlay(
        IEnumerable<(int StrokeIndex, int Par, int GrossA, int GrossB)> holes,
        int courseHandicapA, int courseHandicapB)
    {
        int wonA = 0, wonB = 0, halved = 0;
        foreach (var hole in holes)
        {
            var netA = hole.GrossA - WhsCalculator.StrokesReceivedOnHole(courseHandicapA, hole.StrokeIndex);
            var netB = hole.GrossB - WhsCalculator.StrokesReceivedOnHole(courseHandicapB, hole.StrokeIndex);
            if (netA < netB) wonA++;
            else if (netB < netA) wonB++;
            else halved++;
        }
        return new MatchPlayResult(wonA, wonB, halved);
    }
}
