namespace GolfTrackerApp.Web.Models;

/// <summary>
/// DTO for Round responses to avoid circular references
/// </summary>
public class RoundResponse
{
    public int RoundId { get; set; }
    public int GolfCourseId { get; set; }
    public DateTime DatePlayed { get; set; }
    public int StartingHole { get; set; }
    public int HolesPlayed { get; set; }
    public string RoundType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByApplicationUserId { get; set; } = string.Empty;
    
    // Course information
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    
    // Summary information
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int PlayerCount { get; set; }
    public List<string> PlayingPartners { get; set; } = new();
}

public class ScoreUpdateDto
{
    public int ScoreId { get; set; }
    public int Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
}

public class HoleScoreUpdateDto
{
    public int PlayerId { get; set; }
    public int HoleId { get; set; }
    public int Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
}

public class RoundStatusUpdateDto
{
    public string Status { get; set; } = string.Empty;
}
