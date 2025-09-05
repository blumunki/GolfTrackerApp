namespace GolfTrackerApp.Web.Models;

public class DashboardStats
{
    public int TotalRounds { get; set; }
    public int? BestScore { get; set; }
    public string? BestScoreCourseName { get; set; }
    public DateTime? BestScoreDate { get; set; }
    public double? AverageScore { get; set; }
    public double? AverageToPar { get; set; }
    public int? LowestToPar { get; set; }
    public string? FavoriteCourseName { get; set; }
    public int FavoriteCourseRounds { get; set; }
    public DateTime? LastRoundDate { get; set; }
    public int CurrentStreak { get; set; } // Rounds in a row below/above average
    public bool IsImprovingStreak { get; set; } // True if improving, false if declining
}
