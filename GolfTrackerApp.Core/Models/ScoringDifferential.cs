using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Core.Models;

/// <summary>
/// The WHS score differential for one player's qualifying round, computed by
/// <see cref="Services.WhsCalculator"/>. Inputs (adjusted gross, rating, slope) are
/// snapshotted so the value stays explainable if the tee set is edited later.
/// </summary>
public class ScoringDifferential
{
    public int ScoringDifferentialId { get; set; }

    public int PlayerId { get; set; }

    [ForeignKey("PlayerId")]
    public virtual Player? Player { get; set; }

    public int RoundId { get; set; }

    [ForeignKey("RoundId")]
    public virtual Round? Round { get; set; }

    public int TeeSetId { get; set; }

    [ForeignKey("TeeSetId")]
    public virtual TeeSet? TeeSet { get; set; }

    public int AdjustedGrossScore { get; set; } // after max-score adjustments (par + 5 cap in v1)

    public decimal CourseRating { get; set; }

    public int SlopeRating { get; set; }

    public decimal Differential { get; set; } // (113 / slope) × (AGS − rating), 1 dp

    public bool IsUsedInCalculation { get; set; } // counting towards the current index?

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
