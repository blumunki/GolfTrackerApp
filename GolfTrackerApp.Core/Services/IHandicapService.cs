using GolfTrackerApp.Core.Models;

namespace GolfTrackerApp.Core.Services;

public interface IHandicapService
{
    /// <summary>
    /// Recalculates scoring differentials and the personal WHS index for every player
    /// in the round. No-op for non-qualifying rounds (not completed, not 18 holes, no
    /// tee set with rating/slope, incomplete scorecard). Idempotent — differentials are
    /// upserted per (player, round) and a new HandicapRecord is stored only when the
    /// index changed, so it is safe to call from every completion or recalculation path.
    /// Returns the number of differentials created or recalculated.
    /// </summary>
    Task<int> OnRoundCompletedAsync(int roundId);

    /// <summary>
    /// Runs <see cref="OnRoundCompletedAsync"/> over every completed round, oldest
    /// first, so handicap history evolves in playing order. Idempotent — a repeat run
    /// recalculates the same differentials and creates no new handicap records.
    /// </summary>
    Task<HandicapBackfillResult> BackfillPersonalHandicapsAsync();

    /// <summary>Handicap history for a player, newest first, optionally filtered by source.</summary>
    Task<List<HandicapRecord>> GetHandicapRecordsAsync(int playerId, HandicapSource? source = null);

    /// <summary>
    /// The player's current handicaps: the latest record per source context (personal,
    /// each club, each society). Expired club records are still returned — the consumer
    /// decides how to present <see cref="HandicapRecord.ExpiryDate"/>.
    /// </summary>
    Task<List<HandicapRecord>> GetActiveHandicapsAsync(int playerId);

    /// <summary>The player's scoring differentials for the WHS window (last 20 by round date), newest first.</summary>
    Task<List<ScoringDifferential>> GetRecentDifferentialsAsync(int playerId);

    /// <summary>
    /// Records a manually entered club/regional handicap. Source and IsManualEntry are
    /// forced to ClubRegional/true; the player and club must exist and the index must be
    /// a plausible WHS value.
    /// </summary>
    Task<HandicapRecord> AddManualClubHandicapAsync(HandicapRecord record);

    /// <summary>
    /// Updates a manual club handicap entry (index, dates, club). Returns null when the
    /// record does not exist; throws when the record is calculated rather than manual.
    /// </summary>
    Task<HandicapRecord?> UpdateManualClubHandicapAsync(HandicapRecord record);

    /// <summary>
    /// Deletes a manual club handicap entry. Returns false when the record does not
    /// exist; throws when the record is calculated rather than manual.
    /// </summary>
    Task<bool> DeleteManualClubHandicapAsync(int handicapRecordId);
}
