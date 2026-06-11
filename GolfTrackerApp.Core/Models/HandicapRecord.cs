using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Core.Models;

/// <summary>
/// A point-in-time handicap for a player from one source (personal WHS, club/regional,
/// or society). A new record is stored only when the index changes, so the rows per
/// source form the player's handicap history.
/// </summary>
public class HandicapRecord
{
    public int HandicapRecordId { get; set; }

    public int PlayerId { get; set; }

    [ForeignKey("PlayerId")]
    public virtual Player? Player { get; set; }

    public decimal HandicapIndex { get; set; } // e.g. 18.4; plus handicaps negative

    public HandicapSource Source { get; set; }

    public int? GolfSocietyId { get; set; } // only for Society source

    [ForeignKey("GolfSocietyId")]
    public virtual GolfSociety? GolfSociety { get; set; }

    public int? GolfClubId { get; set; } // only for ClubRegional source

    [ForeignKey("GolfClubId")]
    public virtual GolfClub? GolfClub { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime? ExpiryDate { get; set; } // for club handicaps with renewal

    public string? CalculationDetails { get; set; } // JSON: which rounds, differentials, etc.

    public bool IsManualEntry { get; set; } // true for club handicaps entered by user

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
