namespace GolfTrackerApp.Mobile.Models;

public class GolfCourse
{
    public int GolfCourseId { get; set; }
    public int GolfClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultPar { get; set; }
    public int NumberOfHoles { get; set; }
    
    // Navigation properties (optional for mobile)
    public string? GolfClubName { get; set; }
}
