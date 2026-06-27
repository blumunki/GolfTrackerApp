using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Core.Data;

namespace GolfTrackerApp.Core.Models;

public enum CompetitionStatus
{
    Upcoming,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// A competition hosted by a club or society, played to a scoring format. Rounds are linked
/// to it (Round.CompetitionId) and players register via CompetitionEntry.
/// </summary>
public class Competition
{
    public int CompetitionId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty; // e.g. "Monthly Medal March 2026"

    public int? GolfClubId { get; set; } // host club (nullable — could be a society competition)

    [ForeignKey(nameof(GolfClubId))]
    public virtual GolfClub? GolfClub { get; set; }

    public int? GolfSocietyId { get; set; } // host society (nullable — could be a club competition)

    [ForeignKey(nameof(GolfSocietyId))]
    public virtual GolfSociety? GolfSociety { get; set; }

    public int? GolfCourseId { get; set; } // where it's played

    [ForeignKey(nameof(GolfCourseId))]
    public virtual GolfCourse? GolfCourse { get; set; }

    public ScoringFormat ScoringFormat { get; set; }

    public DateTime Date { get; set; }

    public string? Description { get; set; }

    public bool IsOpen { get; set; } // true = anyone can join; false = members only

    public CompetitionStatus Status { get; set; } = CompetitionStatus.Upcoming;

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Round> Rounds { get; set; } = new List<Round>();
    public virtual ICollection<CompetitionEntry> Entries { get; set; } = new List<CompetitionEntry>();
}
