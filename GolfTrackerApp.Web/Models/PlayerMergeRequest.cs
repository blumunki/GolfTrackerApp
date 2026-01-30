using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

/// <summary>
/// Represents a request to merge a managed player's data into a connected player's profile.
/// This transfers historical round/score data from the source to the target player.
/// </summary>
public class PlayerMergeRequest
{
    public int Id { get; set; }

    /// <summary>
    /// The user who owns the managed player and is requesting the merge.
    /// </summary>
    [Required]
    public string RequestingUserId { get; set; } = string.Empty;

    /// <summary>
    /// The user who will receive the merged data (owner of target player profile).
    /// </summary>
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// The managed player whose data will be transferred (source of data).
    /// </summary>
    [Required]
    public int SourcePlayerId { get; set; }

    /// <summary>
    /// The connected player who will receive the data (destination).
    /// </summary>
    [Required]
    public int TargetPlayerId { get; set; }

    /// <summary>
    /// Current status of the merge request.
    /// </summary>
    public MergeRequestStatus Status { get; set; } = MergeRequestStatus.Pending;

    /// <summary>
    /// When the merge request was created.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the merge was completed, declined, or cancelled.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optional message from the requester explaining the merge.
    /// </summary>
    [MaxLength(500)]
    public string? Message { get; set; }

    /// <summary>
    /// Number of rounds successfully merged (set after completion).
    /// </summary>
    public int RoundsMerged { get; set; }

    /// <summary>
    /// Number of rounds skipped due to duplicates (set after completion).
    /// </summary>
    public int RoundsSkipped { get; set; }

    // Navigation properties
    [ForeignKey("RequestingUserId")]
    public virtual ApplicationUser? RequestingUser { get; set; }

    [ForeignKey("TargetUserId")]
    public virtual ApplicationUser? TargetUser { get; set; }

    [ForeignKey("SourcePlayerId")]
    public virtual Player? SourcePlayer { get; set; }

    [ForeignKey("TargetPlayerId")]
    public virtual Player? TargetPlayer { get; set; }
}
