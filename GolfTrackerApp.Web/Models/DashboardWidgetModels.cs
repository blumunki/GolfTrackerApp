namespace GolfTrackerApp.Web.Models;

public class ScoreDistributionBucket
{
    public string Range { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class FavoriteCourseItem
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int RoundsPlayed { get; set; }
    public double AverageScore { get; set; }
}
