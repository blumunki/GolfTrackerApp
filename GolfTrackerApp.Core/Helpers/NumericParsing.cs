using System.Globalization;

namespace GolfTrackerApp.Core.Helpers;

/// <summary>
/// Lenient numeric parsing for imported CSV cells. Blank cells and common
/// "no value" sentinels ("N/A", "NA", "-", "TBC", …) — and anything that
/// doesn't parse — become null, so a single unrated tee never aborts a whole
/// import the way a strict typed CSV column would.
/// </summary>
public static class NumericParsing
{
    private static readonly HashSet<string> NullSentinels = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "n/a", "n/a.", "na", "-", "--", "—", "tbc", "tbd", "none", "null", "?",
    };

    public static bool IsNullSentinel(string? value) =>
        value is null || NullSentinels.Contains(value.Trim());

    public static decimal? ParseNullableDecimal(string? value) =>
        IsNullSentinel(value)
            ? null
            : decimal.TryParse(value!.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                ? d
                : null;

    public static int? ParseNullableInt(string? value) =>
        IsNullSentinel(value)
            ? null
            : int.TryParse(value!.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                ? i
                : null;
}
