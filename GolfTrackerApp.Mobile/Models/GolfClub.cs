namespace GolfTrackerApp.Mobile.Models;

public class GolfClub
{
    public int GolfClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? CountyOrRegion { get; set; }
    public string? Postcode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }

    // This property will be populated from the API when fetching a single club's details.
    public List<GolfCourse> Courses { get; set; } = new();

    // This property is sent by the API in the main list view to efficiently show the count.
    public int CourseCount { get; set; }
}