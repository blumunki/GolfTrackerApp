namespace GolfTrackerApp.Core.Models;

public enum HandicapSource
{
    /// <summary>Auto-calculated from all qualifying rounds (WHS).</summary>
    Personal,

    /// <summary>Official handicap from a club/national body (manually entered or synced).</summary>
    ClubRegional,

    /// <summary>Calculated from rounds linked to a society's competitions.</summary>
    Society
}
