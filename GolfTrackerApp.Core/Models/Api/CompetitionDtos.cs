namespace GolfTrackerApp.Core.Models.Api;

public class CompetitionSummaryDto
{
    public int CompetitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GolfClubName { get; set; }
    public int? GolfClubId { get; set; }
    public string? GolfSocietyName { get; set; }
    public int? GolfSocietyId { get; set; }
    public string? GolfCourseName { get; set; }
    public int? GolfCourseId { get; set; }
    public string ScoringFormat { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public int EntryCount { get; set; }
}

public class CompetitionDetailDto : CompetitionSummaryDto
{
    public string? Description { get; set; }
    public List<CompetitionEntryDto> Entries { get; set; } = new();
    public List<int> LinkedRoundIds { get; set; } = new();
}

public class CompetitionEntryDto
{
    public int CompetitionEntryId { get; set; }
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string? TeeSetName { get; set; }
    public decimal? HandicapAtEntry { get; set; }
    public int? GrossScore { get; set; }
    public int? NetScore { get; set; }
    public int? StablefordPoints { get; set; }
    public int? Position { get; set; }
}

public class CreateCompetitionRequest
{
    public string Name { get; set; } = string.Empty;
    public int? GolfClubId { get; set; }
    public int? GolfSocietyId { get; set; }
    public int? GolfCourseId { get; set; }
    public string ScoringFormat { get; set; } = "Medal";
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public bool IsOpen { get; set; }
}

public class UpdateCompetitionRequest
{
    public string Name { get; set; } = string.Empty;
    public int? GolfCourseId { get; set; }
    public string ScoringFormat { get; set; } = "Medal";
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public bool IsOpen { get; set; }
}

public class CompetitionStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CompetitionEntryRequest
{
    public int PlayerId { get; set; }
    public int? TeeSetId { get; set; }
}
