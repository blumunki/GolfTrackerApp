namespace GolfTrackerApp.Web.Models;

public class CourseHistoryItem
{
    public int GolfCourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public DateTime LastPlayedDate { get; set; }
    public int MostRecentScore { get; set; }
    public int MostRecentToPar { get; set; }
    public int BestScore { get; set; }
    public int BestToPar { get; set; }
    public int TimesPlayed { get; set; }
}
