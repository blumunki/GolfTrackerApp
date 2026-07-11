using GolfTrackerApp.Core.Models;

namespace GolfTrackerApp.Core.Services;

/// <summary>
/// Competition lifecycle, entries and round linking. The service enforces data integrity
/// (host exclusivity, entry uniqueness, round ownership); WHO may call create/manage is
/// enforced at the controller/page per the interim authorization model — global admin +
/// society Owner/Admin (ARCHITECTURE §12.5 3.5).
/// </summary>
public interface ICompetitionService
{
    /// <summary>
    /// Creates a competition. Requires a name, date and CreatedByUserId; a competition may be
    /// hosted by a club OR a society (or neither, for ad-hoc/open events) but never both.
    /// Referenced club/society/course must exist.
    /// </summary>
    Task<Competition> CreateCompetitionAsync(Competition competition);

    /// <summary>The competition with host, course, entries (players/tees) and linked rounds; null when missing.</summary>
    Task<Competition?> GetCompetitionByIdAsync(int competitionId);

    /// <summary>Competitions newest-date-first, optionally filtered by host club, host society and/or status.</summary>
    Task<List<Competition>> GetCompetitionsAsync(
        int? golfClubId = null, int? golfSocietyId = null, CompetitionStatus? status = null);

    /// <summary>
    /// Updates name/format/date/description/IsOpen/course. Returns null when missing; throws
    /// when the competition is Completed or Cancelled (terminal). Hosts cannot be changed.
    /// </summary>
    Task<Competition?> UpdateCompetitionAsync(Competition competition);

    /// <summary>
    /// Moves the competition to a new status. Completed and Cancelled are terminal.
    /// Returns null when missing.
    /// </summary>
    Task<Competition?> SetStatusAsync(int competitionId, CompetitionStatus status);

    /// <summary>
    /// Enters a player, snapshotting their current display handicap as HandicapAtEntry.
    /// Throws when the competition is terminal or the player is already entered.
    /// </summary>
    Task<CompetitionEntry> AddEntryAsync(int competitionId, int playerId, int? teeSetId = null);

    /// <summary>Withdraws a player's entry. False when no such entry; throws when the competition is Completed.</summary>
    Task<bool> RemoveEntryAsync(int competitionId, int playerId);

    /// <summary>
    /// Links one of the requesting user's own rounds (admins may link any round) to a
    /// non-cancelled competition. Returns the updated round; null when round or competition
    /// is missing; throws UnauthorizedAccessException when the round isn't theirs.
    /// </summary>
    Task<Round?> AssignRoundAsync(int roundId, int competitionId, string requestingUserId, bool isUserAdmin);

    /// <summary>Removes a round's competition link, with the same ownership rule as assignment.</summary>
    Task<Round?> UnassignRoundAsync(int roundId, string requestingUserId, bool isUserAdmin);

    /// <summary>
    /// Competitions the player is entered in that can still accept rounds (Upcoming or
    /// InProgress), newest date first — the options for the round-recording selector.
    /// </summary>
    Task<List<Competition>> GetCompetitionsForPlayerAsync(int playerId);

    /// <summary>
    /// Whether the user manages the given host under the interim model (§12.5 3.5):
    /// society Owner/Admin for society-hosted competitions; club-hosted and ad-hoc return
    /// false (global-admin only — the caller ORs in the Admin role check).
    /// </summary>
    Task<bool> IsHostManagerAsync(int? golfClubId, int? golfSocietyId, string userId);

    /// <summary>
    /// Computes and persists results for every entry from the players' linked rounds:
    /// gross (sum of strokes), Medal net and Stableford points via ScoringCalculator
    /// (course handicap from HandicapAtEntry and the tee's rating/slope; scratch when
    /// either is missing), and competition-style positions (ties share, e.g. 1,2,2,4 —
    /// Stableford ranks by points descending, other formats by net ascending). Entries
    /// whose player has no linked round get null results and no position. Idempotent —
    /// safe to recompute at any time. Returns entries in ranked order.
    /// </summary>
    Task<List<CompetitionEntry>> ComputeResultsAsync(int competitionId);
}
