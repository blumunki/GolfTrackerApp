using System.Text.Json.Serialization;

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

    // Maps from the API's "golfCourses" JSON property
    [JsonPropertyName("golfCourses")]
    public List<GolfCourse>? Courses { get; set; }

    // Derive course count from the courses list
    [JsonIgnore]
    public int CourseCount => Courses?.Count ?? 0;
}