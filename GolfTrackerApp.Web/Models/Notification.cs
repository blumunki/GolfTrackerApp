using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

/// <summary>
/// Represents a notification for a user.
/// Used for connection requests, merge requests, and system messages.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// The user who should see this notification.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The type of notification.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Short title for the notification.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message content.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL to navigate to when notification is clicked.
    /// </summary>
    [MaxLength(200)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// ID of the related entity (ConnectionId or MergeRequestId).
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}
