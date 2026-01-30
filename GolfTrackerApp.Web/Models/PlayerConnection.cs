using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

/// <summary>
/// Represents a connection/friendship between two users.
/// Connections are bidirectional once accepted.
/// </summary>
public class PlayerConnection
{
    public int Id { get; set; }

    /// <summary>
    /// The user who initiated the connection request.
    /// </summary>
    [Required]
    public string RequestingUserId { get; set; } = string.Empty;

    /// <summary>
    /// The user who received the connection request.
    /// </summary>
    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the connection request.
    /// </summary>
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

    /// <summary>
    /// When the connection request was sent.
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the request was accepted or declined.
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    [ForeignKey("RequestingUserId")]
    public virtual ApplicationUser? RequestingUser { get; set; }

    [ForeignKey("TargetUserId")]
    public virtual ApplicationUser? TargetUser { get; set; }
}
