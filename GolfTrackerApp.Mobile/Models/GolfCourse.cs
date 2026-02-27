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
    
    // Navigation property for nested deserialization from API
    [JsonPropertyName("golfClub")]
    public GolfClubRef? GolfClub { get; set; }
    
    // Convenience accessor
    public string? GolfClubName => GolfClub?.Name;
}

public class GolfClubRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("golfClubId")]
    public int GolfClubId { get; set; }
}
