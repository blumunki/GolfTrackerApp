namespace GolfTrackerApp.Web.Models;

public class PlayerPerformanceDataPoint
{
    public int RoundId { get; set; }
    public DateTime Date { get; set; }
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int ScoreVsPar => TotalScore - TotalPar;
    public string CourseName { get; set; } = string.Empty;
    public int HolesPlayed { get; set; }
}

public class PlayerQuickStats
{
    public int PlayerId { get; set; }
    public int RoundCount { get; set; }
    public int? BestScore { get; set; }
    public double? AverageScore { get; set; }
    public DateTime? LastPlayed { get; set; }
    public double? AverageVsPar { get; set; }
}