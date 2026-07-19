namespace GolfTrackerApp.Core.Models;

/// <summary>The current user's self-registration state for one competition.</summary>
public sealed class CompetitionRegistrationStatus
{
    public int CompetitionId { get; set; }
    public int? PlayerId { get; set; }
    public bool IsRegistered { get; set; }
    public bool IsEligible { get; set; }
    public bool HasLinkedRound { get; set; }
    public bool CanRegister { get; set; }
    public bool CanWithdraw { get; set; }
    public string Message { get; set; } = string.Empty;
}
