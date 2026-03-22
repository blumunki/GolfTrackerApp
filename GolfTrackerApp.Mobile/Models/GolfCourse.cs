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

    [JsonPropertyName("teeSets")]
    public List<TeeSetDto>? TeeSets { get; set; }
}

public class TeeSetDto
{
    [JsonPropertyName("teeSetId")]
    public int TeeSetId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("colour")]
    public string Colour { get; set; } = "#FFD700";

    [JsonPropertyName("courseRating")]
    public decimal? CourseRating { get; set; }

    [JsonPropertyName("slopeRating")]
    public int? SlopeRating { get; set; }

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "Unisex";

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("holeTees")]
    public List<HoleTeeDto>? HoleTees { get; set; }
}

public class HoleTeeDto
{
    [JsonPropertyName("holeTeeId")]
    public int HoleTeeId { get; set; }

    [JsonPropertyName("holeId")]
    public int HoleId { get; set; }

    [JsonPropertyName("par")]
    public int Par { get; set; }

    [JsonPropertyName("strokeIndex")]
    public int? StrokeIndex { get; set; }

    [JsonPropertyName("lengthYards")]
    public int? LengthYards { get; set; }
}

public class GolfClubRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("golfClubId")]
    public int GolfClubId { get; set; }
}
