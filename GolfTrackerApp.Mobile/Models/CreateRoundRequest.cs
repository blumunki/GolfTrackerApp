using System.Text.Json.Serialization;

namespace GolfTrackerApp.Mobile.Models;

public enum RoundTypeOption
{
    Friendly,
    Competitive
}

public enum RoundCompletionStatus
{
    InProgress,
    Completed,
    Abandoned
}

public class CreateRoundRequest
{
    [JsonPropertyName("golfCourseId")]
    public int GolfCourseId { get; set; }
    
    [JsonPropertyName("datePlayed")]
    public DateTime DatePlayed { get; set; }
    
    [JsonPropertyName("startingHole")]
    public int StartingHole { get; set; } = 1;
    
    [JsonPropertyName("holesPlayed")]
    public int HolesPlayed { get; set; } = 18;
    
    [JsonPropertyName("roundType")]
    public RoundTypeOption RoundType { get; set; } = RoundTypeOption.Friendly;
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    [JsonPropertyName("status")]
    public RoundCompletionStatus Status { get; set; } = RoundCompletionStatus.InProgress;
    
    [JsonPropertyName("createdByApplicationUserId")]
    public string CreatedByApplicationUserId { get; set; } = string.Empty;
    
    [JsonPropertyName("roundPlayers")]
    public List<CreateRoundPlayerRequest> RoundPlayers { get; set; } = new();
    
    [JsonPropertyName("scores")]
    public List<CreateScoreRequest> Scores { get; set; } = new();
}

public class CreateRoundPlayerRequest
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }
}

public class CreateScoreRequest
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }
    
    [JsonPropertyName("holeId")]
    public int HoleId { get; set; }
    
    [JsonPropertyName("strokes")]
    public int Strokes { get; set; }
    
    [JsonPropertyName("putts")]
    public int? Putts { get; set; }
    
    [JsonPropertyName("fairwayHit")]
    public bool? FairwayHit { get; set; }
}

public class HoleScoreEntry
{
    public int HoleId { get; set; }
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int StrokeIndex { get; set; }
    public int? Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
    
    public int? ScoreVsPar => Strokes.HasValue && Par != 0 ? Strokes - Par : null;
}
