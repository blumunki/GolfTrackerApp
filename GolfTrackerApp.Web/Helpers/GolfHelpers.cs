namespace GolfTrackerApp.Web.Helpers;

public static class GolfHelpers
{
    public static string FormatVsPar(int vsPar) =>
        vsPar == 0 ? "E" : vsPar > 0 ? $"+{vsPar}" : vsPar.ToString();

    public static string FormatVsPar(double? vsPar)
    {
        if (!vsPar.HasValue) return "-";
        return vsPar.Value switch
        {
            0 => "E",
            > 0 => $"+{vsPar.Value:F1}",
            _ => vsPar.Value.ToString("F1")
        };
    }

    public static string GetVsParClass(int vsPar) =>
        vsPar < 0 ? "under-par" : vsPar > 0 ? "over-par" : "even-par";
}
