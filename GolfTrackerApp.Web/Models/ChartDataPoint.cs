namespace GolfTrackerApp.Web.Models;

public class PlayerPerformanceDataPoint
{
    public DateTime Date { get; set; }
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int ScoreVsPar => TotalScore - TotalPar;
    public string CourseName { get; set; } = string.Empty;
    public int HolesPlayed { get; set; }
}