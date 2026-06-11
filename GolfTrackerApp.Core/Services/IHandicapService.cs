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
}
