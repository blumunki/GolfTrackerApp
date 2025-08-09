namespace GolfTrackerApp.Web.Models;

public class ChartDataPoint
{
    public DateTime Date { get; set; }
    public int TotalScore { get; set; }
    public string CourseName { get; set; } = string.Empty;
}