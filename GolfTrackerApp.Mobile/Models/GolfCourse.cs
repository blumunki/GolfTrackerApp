using System.Text.Json.Serialization;

namespace GolfTrackerApp.Mobile.Models;

public class GolfCourse
{
    [JsonPropertyName("golfCourseId")]
    public int GolfCourseId { get; set; }
    
    [JsonPropertyName("golfClubId")]
    public int GolfClubId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("defaultPar")]
    public int DefaultPar { get; set; }
    
    [JsonPropertyName("numberOfHoles")]
    public int NumberOfHoles { get; set; }
    
    // Navigation properties (optional for mobile)
    [JsonPropertyName("golfClubName")]
    public string? GolfClubName { get; set; }
}
