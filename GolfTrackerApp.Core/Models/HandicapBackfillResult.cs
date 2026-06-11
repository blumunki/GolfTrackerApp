namespace GolfTrackerApp.Core.Models;

/// <summary>Outcome of a personal-handicap backfill run over historical rounds.</summary>
public class HandicapBackfillResult
{
    /// <summary>Completed rounds examined (the "m" in n-of-m).</summary>
    public int RoundsProcessed { get; set; }

    /// <summary>Rounds that yielded at least one scoring differential (the "n").</summary>
    public int RoundsQualified { get; set; }

    /// <summary>Differentials created or recalculated across all players.</summary>
    public int DifferentialsWritten { get; set; }

    /// <summary>New handicap records stored (0 on a repeat run — indexes unchanged).</summary>
    public int HandicapRecordsCreated { get; set; }

    /// <summary>Players that now have a personal handicap index.</summary>
    public int PlayersWithIndex { get; set; }
}
