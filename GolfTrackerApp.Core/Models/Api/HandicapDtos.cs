namespace GolfTrackerApp.Core.Models.Api;

public class HandicapRecordDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public decimal HandicapIndex { get; set; }
    public string Source { get; set; } = string.Empty;
    public int? GolfClubId { get; set; }
    public string? GolfClubName { get; set; }
    public int? GolfSocietyId { get; set; }
    public string? GolfSocietyName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsManualEntry { get; set; }
}

public class ScoringDifferentialDto
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string? CourseName { get; set; }
    public string? TeeName { get; set; }
    public int AdjustedGrossScore { get; set; }
    public decimal CourseRating { get; set; }
    public int SlopeRating { get; set; }
    public decimal Differential { get; set; }
    public bool IsUsedInCalculation { get; set; }
}

public class ClubHandicapRequestDto
{
    public int PlayerId { get; set; }
    public int GolfClubId { get; set; }
    public decimal HandicapIndex { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
