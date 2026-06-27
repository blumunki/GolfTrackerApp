using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Core.Models;

/// <summary>One player's entry in a competition, including their result once scored.</summary>
public class CompetitionEntry
{
    public int CompetitionEntryId { get; set; }

    public int CompetitionId { get; set; }

    [ForeignKey(nameof(CompetitionId))]
    public virtual Competition? Competition { get; set; }

    public int PlayerId { get; set; }

    [ForeignKey(nameof(PlayerId))]
    public virtual Player? Player { get; set; }

    public int? TeeSetId { get; set; }

    [ForeignKey(nameof(TeeSetId))]
    public virtual TeeSet? TeeSet { get; set; }

    public decimal? HandicapAtEntry { get; set; } // snapshot of the handicap used

    public int? GrossScore { get; set; }
    public int? NetScore { get; set; }
    public int? StablefordPoints { get; set; }
    public int? Position { get; set; }
}
