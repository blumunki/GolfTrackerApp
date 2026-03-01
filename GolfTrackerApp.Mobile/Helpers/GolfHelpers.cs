namespace GolfTrackerApp.Mobile.Helpers;

public static class GolfHelpers
{
    public static string FormatVsPar(int vsPar) =>
        vsPar == 0 ? "E" : vsPar > 0 ? $"+{vsPar}" : vsPar.ToString();

    /// <summary>
    /// Returns inline style string with color only (no background).
    /// </summary>
    public static string GetVsParStyle(int vsPar)
    {
        if (vsPar < 0) return "color:#16a34a;";
        if (vsPar == 0) return "color:#2563eb;";
        return "color:#dc2626;";
    }

    /// <summary>
    /// Returns inline style string with background + color (for badge display).
    /// </summary>
    public static string GetVsParBadgeStyle(int vsPar)
    {
        if (vsPar < 0) return "background:#dcfce7; color:#16a34a;";
        if (vsPar == 0) return "background:#dbeafe; color:#2563eb;";
        return "background:#fef2f2; color:#dc2626;";
    }
}
