using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<NotificationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(Notification notification)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        notification.CreatedAt = DateTime.UtcNow;
        notification.IsRead = false;
        
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
            notification.Id, notification.UserId);
        
        return notification;
    }

    public async Task<Notification> CreateConnectionRequestNotificationAsync(
        string targetUserId, string requesterName, int connectionId)
    {
        var notification = new Notification
        {
            UserId = targetUserId,
            Type = NotificationType.ConnectionRequest,
            Title = "New Connection Request",
            Message = $"{requesterName} wants to connect with you.",
            ActionUrl = "/players",
            RelatedEntityId = connectionId
        };
        
        return await CreateNotificationAsync(notification);
    }

    public async Task<Notification> CreateConnectionAcceptedNotificationAsync(
        string requesterId, string accepterName, int connectionId)
    {
        var notification = new Notification
        {
            UserId = requesterId,
            Type = NotificationType.ConnectionAccepted,
            Title = "Connection Accepted",
            Message = $"{accepterName} accepted your connection request.",
            ActionUrl = "/players",
            RelatedEntityId = connectionId
        };
        
        return await CreateNotificationAsync(notification);
    }

    public async Task<Notification> CreateMergeRequestNotificationAsync(
        string targetUserId, string requesterName, string sourcePlayerName, int mergeRequestId)
    {
        var notification = new Notification
        {
            UserId = targetUserId,
            Type = NotificationType.MergeRequest,
            Title = "Data Transfer Request",
            Message = $"{requesterName} wants to transfer score data for \"{sourcePlayerName}\" to your profile.",
            ActionUrl = "/players",
            RelatedEntityId = mergeRequestId
        };
        
        return await CreateNotificationAsync(notification);
    }

    public async Task<Notification> CreateMergeCompletedNotificationAsync(
        string requesterId, string accepterName, int roundsMerged, int roundsSkipped, int mergeRequestId)
    {
        var skippedText = roundsSkipped > 0 ? $" ({roundsSkipped} skipped as duplicates)" : "";
        var notification = new Notification
        {
            UserId = requesterId,
            Type = NotificationType.MergeCompleted,
            Title = "Data Transfer Complete",
            Message = $"{accepterName} accepted your data transfer. {roundsMerged} rounds merged{skippedText}.",
            ActionUrl = "/players",
            RelatedEntityId = mergeRequestId
        };
        
        return await CreateNotificationAsync(notification);
    }

    public async Task<List<Notification>> GetNotificationsAsync(string userId, int take = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        
        if (notification == null)
        {
            return false;
        }
        
        notification.IsRead = true;
        await context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));
        
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        
        if (notification == null)
        {
            return false;
        }
        
        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();
        
        return true;
    }
}
