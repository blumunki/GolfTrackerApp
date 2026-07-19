using System.Text.Json.Serialization;

namespace GolfTrackerApp.Mobile.Models;

public class CompetitionSummaryDto
{
    [JsonPropertyName("competitionId")]
    public int CompetitionId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("golfClubName")]
    public string? GolfClubName { get; set; }

    [JsonPropertyName("golfClubId")]
    public int? GolfClubId { get; set; }

    [JsonPropertyName("golfSocietyName")]
    public string? GolfSocietyName { get; set; }

    [JsonPropertyName("golfSocietyId")]
    public int? GolfSocietyId { get; set; }

    [JsonPropertyName("golfCourseName")]
    public string? GolfCourseName { get; set; }

    [JsonPropertyName("golfCourseId")]
    public int? GolfCourseId { get; set; }

    [JsonPropertyName("scoringFormat")]
    public string ScoringFormat { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }

    [JsonPropertyName("entryCount")]
    public int EntryCount { get; set; }
}

public class CompetitionDetailDto : CompetitionSummaryDto
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("entries")]
    public List<CompetitionEntryDto> Entries { get; set; } = new();

    [JsonPropertyName("linkedRoundIds")]
    public List<int> LinkedRoundIds { get; set; } = new();
}

public class CompetitionEntryDto
{
    [JsonPropertyName("competitionEntryId")]
    public int CompetitionEntryId { get; set; }

    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = string.Empty;

    [JsonPropertyName("teeSetName")]
    public string? TeeSetName { get; set; }

    [JsonPropertyName("handicapAtEntry")]
    public decimal? HandicapAtEntry { get; set; }

    [JsonPropertyName("grossScore")]
    public int? GrossScore { get; set; }

    [JsonPropertyName("netScore")]
    public int? NetScore { get; set; }

    [JsonPropertyName("stablefordPoints")]
    public int? StablefordPoints { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }
}

public class CompetitionRegistrationStatusDto
{
    [JsonPropertyName("competitionId")]
    public int CompetitionId { get; set; }

    [JsonPropertyName("playerId")]
    public int? PlayerId { get; set; }

    [JsonPropertyName("isRegistered")]
    public bool IsRegistered { get; set; }

    [JsonPropertyName("isEligible")]
    public bool IsEligible { get; set; }

    [JsonPropertyName("hasLinkedRound")]
    public bool HasLinkedRound { get; set; }

    [JsonPropertyName("canRegister")]
    public bool CanRegister { get; set; }

    [JsonPropertyName("canWithdraw")]
    public bool CanWithdraw { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class CreateCompetitionRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("golfClubId")]
    public int? GolfClubId { get; set; }

    [JsonPropertyName("golfSocietyId")]
    public int? GolfSocietyId { get; set; }

    [JsonPropertyName("golfCourseId")]
    public int? GolfCourseId { get; set; }

    [JsonPropertyName("scoringFormat")]
    public string ScoringFormat { get; set; } = "Medal";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }
}

public class UpdateCompetitionRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("golfCourseId")]
    public int? GolfCourseId { get; set; }

    [JsonPropertyName("scoringFormat")]
    public string ScoringFormat { get; set; } = "Medal";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }
}

public class CompetitionStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class CompetitionEntryRequest
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }

    [JsonPropertyName("teeSetId")]
    public int? TeeSetId { get; set; }
}

public class CompetitionRegistrationRequest
{
    [JsonPropertyName("teeSetId")]
    public int? TeeSetId { get; set; }
}
