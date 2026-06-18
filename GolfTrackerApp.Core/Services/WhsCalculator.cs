namespace GolfTrackerApp.Core.Services;

/// <summary>
/// Pure World Handicap System calculations — no data access. Persistence and
/// round-completion triggers live in the handicap service (WORKLOG 2-3); the
/// rules implemented here are specified in docs/ARCHITECTURE.md §12.5 (4.3).
/// </summary>
public static class WhsCalculator
{
    /// <summary>Slope rating of a course of standard difficulty.</summary>
    public const decimal StandardSlope = 113m;

    /// <summary>WHS maximum handicap index.</summary>
    public const decimal MaxIndex = 54.0m;

    /// <summary>How many of the most recent differentials an index is computed over.</summary>
    public const int DifferentialWindow = 20;

    /// <summary>Per-hole cap above par for adjusted gross score (v1: players without an established index).</summary>
    public const int MaxStrokesOverPar = 5;

    /// <summary>
    /// Score differential for one round: (113 / slope) × (adjusted gross − course rating),
    /// rounded to 1 decimal (half away from zero).
    /// </summary>
    public static decimal ComputeDifferential(int adjustedGrossScore, decimal courseRating, decimal slopeRating)
    {
        if (slopeRating <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slopeRating), slopeRating, "Slope rating must be positive.");
        }

        var differential = StandardSlope / slopeRating * (adjustedGrossScore - courseRating);
        return Math.Round(differential, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Handicap index from score differentials ordered most recent first; only the first
    /// <see cref="DifferentialWindow"/> are considered. Returns null when fewer than 3
    /// differentials exist (no index can be established). Result is rounded to 1 decimal
    /// and capped at <see cref="MaxIndex"/>; plus handicaps are negative.
    /// </summary>
    public static decimal? ComputeIndex(IEnumerable<decimal> differentialsMostRecentFirst)
    {
        var window = differentialsMostRecentFirst.Take(DifferentialWindow).ToList();
        var (lowestCount, adjustment) = GetTableEntry(window.Count);

        if (lowestCount == 0)
        {
            return null;
        }

        var average = window.OrderBy(d => d).Take(lowestCount).Average();
        var index = Math.Round(average - adjustment, 1, MidpointRounding.AwayFromZero);
        return Math.Min(index, MaxIndex);
    }

    /// <summary>
    /// How many of the lowest differentials count towards the index for a window of
    /// the given size (the "lowest k" column of the WHS table; 0 when no index exists).
    /// </summary>
    public static int CountingDifferentialsCount(int differentialCount) =>
        GetTableEntry(Math.Min(differentialCount, DifferentialWindow)).LowestCount;

    /// <summary>WHS table: how many lowest differentials count, and the flat adjustment.</summary>
    private static (int LowestCount, decimal Adjustment) GetTableEntry(int differentialCount) =>
        differentialCount switch
        {
            < 3 => (0, 0m),
            3 => (1, 2.0m),
            4 => (1, 1.0m),
            5 => (1, 0m),
            6 => (2, 1.0m),
            7 or 8 => (2, 0m),
            >= 9 and <= 11 => (3, 0m),
            >= 12 and <= 14 => (4, 0m),
            15 or 16 => (5, 0m),
            17 or 18 => (6, 0m),
            19 => (7, 0m),
            _ => (8, 0m),
        };

    /// <summary>Holes in a standard round, used to distribute the course handicap.</summary>
    public const int RoundHoles = 18;

    /// <summary>
    /// Adjusted gross score, v1 fallback: each hole capped at par + <see cref="MaxStrokesOverPar"/>.
    /// Used for players without an established index (and as a safety net).
    /// </summary>
    public static int ComputeAdjustedGrossScore(IEnumerable<(int Par, int Strokes)> holes) =>
        holes.Sum(h => Math.Min(h.Strokes, h.Par + MaxStrokesOverPar));

    /// <summary>
    /// Course Handicap = Index × (Slope / 113) + (Course Rating − Par), rounded to the
    /// nearest whole stroke. The strokes a player receives on this course/tee.
    /// </summary>
    public static int ComputeCourseHandicap(decimal handicapIndex, int slopeRating, decimal courseRating, int par)
    {
        var courseHandicap = handicapIndex * slopeRating / StandardSlope + (courseRating - par);
        return (int)Math.Round(courseHandicap, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Strokes a player receives on one hole: the course handicap spread evenly across the
    /// <see cref="RoundHoles"/> holes, with the remainder given to the lowest stroke-index
    /// holes. Plus/zero handicaps receive none (v1 does not remove strokes).
    /// </summary>
    public static int StrokesReceivedOnHole(int courseHandicap, int strokeIndex, int holeCount = RoundHoles)
    {
        if (courseHandicap <= 0)
        {
            return 0;
        }

        var baseStrokes = courseHandicap / holeCount;
        var remainder = courseHandicap % holeCount;
        return baseStrokes + (strokeIndex <= remainder ? 1 : 0);
    }

    /// <summary>
    /// Adjusted gross score, v2 (proper WHS): each hole capped at net double bogey —
    /// par + 2 + strokes received on that hole, given the player's course handicap.
    /// </summary>
    public static int ComputeAdjustedGrossScore(
        IEnumerable<(int Par, int Strokes, int StrokeIndex)> holes, int courseHandicap) =>
        holes.Sum(h => Math.Min(
            h.Strokes,
            h.Par + 2 + StrokesReceivedOnHole(courseHandicap, h.StrokeIndex)));
}
