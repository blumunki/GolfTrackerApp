using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface INotificationService
{
    /// <summary>
    /// Creates a new notification for a user.
    /// </summary>
    Task<Notification> CreateNotificationAsync(Notification notification);
    
    /// <summary>
    /// Creates a notification for a connection request.
    /// </summary>
    Task<Notification> CreateConnectionRequestNotificationAsync(string targetUserId, string requesterName, int connectionId);
    
    /// <summary>
    /// Creates a notification for an accepted connection.
    /// </summary>
    Task<Notification> CreateConnectionAcceptedNotificationAsync(string requesterId, string accepterName, int connectionId);
    
    /// <summary>
    /// Creates a notification for a merge request.
    /// </summary>
    Task<Notification> CreateMergeRequestNotificationAsync(string targetUserId, string requesterName, string sourcePlayerName, int mergeRequestId);
    
    /// <summary>
    /// Creates a notification for a completed merge.
    /// </summary>
    Task<Notification> CreateMergeCompletedNotificationAsync(string requesterId, string accepterName, int roundsMerged, int roundsSkipped, int mergeRequestId);
    
    /// <summary>
    /// Gets notifications for a user, ordered by most recent.
    /// </summary>
    Task<List<Notification>> GetNotificationsAsync(string userId, int take = 20);
    
    /// <summary>
    /// Gets unread notifications for a user.
    /// </summary>
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
    
    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);
    
    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    
    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task<bool> MarkAllAsReadAsync(string userId);
    
    /// <summary>
    /// Deletes a notification.
    /// </summary>
    Task<bool> DeleteNotificationAsync(int notificationId, string userId);
}
