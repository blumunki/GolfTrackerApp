namespace GolfTrackerApp.Web.Models;

// This class will hold all the data needed by the PlayerReport.razor page
public class PlayerReportViewModel
{
    public Player? Player { get; set; }
    public List<GolfCourse> FilterCourses { get; set; } = new();
    public List<PlayerPerformanceDataPoint> PerformanceData { get; set; } = new();
}