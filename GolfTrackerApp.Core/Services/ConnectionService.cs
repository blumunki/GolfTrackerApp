using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class ConnectionService : IConnectionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConnectionService> _logger;

    public ConnectionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        INotificationService notificationService,
        ILogger<ConnectionService> logger)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PlayerConnection> SendConnectionRequestAsync(string requestingUserId, string targetUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check if connection already exists (in either direction)
        var existingConnection = await context.PlayerConnections
            .FirstOrDefaultAsync(c =>
                (c.RequestingUserId == requestingUserId && c.TargetUserId == targetUserId) ||
                (c.RequestingUserId == targetUserId && c.TargetUserId == requestingUserId));

        if (existingConnection != null)
        {
            throw new InvalidOperationException("A connection request already exists between these users.");
        }

        var connection = new PlayerConnection
        {
            RequestingUserId = requestingUserId,
            TargetUserId = targetUserId,
            Status = ConnectionStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        context.PlayerConnections.Add(connection);
        await context.SaveChangesAsync();

        // Get requester name for notification
        var requester = await context.Users.FindAsync(requestingUserId);
        var requesterName = requester?.UserName ?? "Someone";

        await _notificationService.CreateConnectionRequestNotificationAsync(
            targetUserId, requesterName, connection.Id);

        _logger.LogInformation("Connection request sent from {RequesterId} to {TargetId}",
            requestingUserId, targetUserId);

        return connection;
    }

    public async Task<PlayerConnection?> AcceptConnectionRequestAsync(int connectionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var connection = await context.PlayerConnections
            .Include(c => c.RequestingUser)
            .Include(c => c.TargetUser)
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TargetUserId == userId);

        if (connection == null || connection.Status != ConnectionStatus.Pending)
        {
            return null;
        }

        connection.Status = ConnectionStatus.Accepted;
        connection.RespondedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Notify the requester
        var accepterName = connection.TargetUser?.UserName ?? "Someone";
        await _notificationService.CreateConnectionAcceptedNotificationAsync(
            connection.RequestingUserId, accepterName, connection.Id);

        _logger.LogInformation("Connection {ConnectionId} accepted by {UserId}", connectionId, userId);

        return connection;
    }

    public async Task<PlayerConnection?> DeclineConnectionRequestAsync(int connectionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var connection = await context.PlayerConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TargetUserId == userId);

        if (connection == null || connection.Status != ConnectionStatus.Pending)
        {
            return null;
        }

        connection.Status = ConnectionStatus.Declined;
        connection.RespondedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Connection {ConnectionId} declined by {UserId}", connectionId, userId);

        return connection;
    }

    public async Task<bool> CancelConnectionRequestAsync(int connectionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var connection = await context.PlayerConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && 
                                      c.RequestingUserId == userId && 
                                      c.Status == ConnectionStatus.Pending);

        if (connection == null)
        {
            return false;
        }

        context.PlayerConnections.Remove(connection);
        await context.SaveChangesAsync();

        _logger.LogInformation("Connection request {ConnectionId} cancelled by {UserId}", connectionId, userId);

        return true;
    }

    public async Task<bool> RemoveConnectionAsync(int connectionId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var connection = await context.PlayerConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId &&
                (c.RequestingUserId == userId || c.TargetUserId == userId) &&
                c.Status == ConnectionStatus.Accepted);

        if (connection == null)
        {
            return false;
        }

        context.PlayerConnections.Remove(connection);
        await context.SaveChangesAsync();

        _logger.LogInformation("Connection {ConnectionId} removed by {UserId}", connectionId, userId);

        return true;
    }

    public async Task<List<PlayerConnection>> GetConnectionsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerConnections
            .Include(c => c.RequestingUser)
            .Include(c => c.TargetUser)
            .Where(c => (c.RequestingUserId == userId || c.TargetUserId == userId) &&
                        c.Status == ConnectionStatus.Accepted)
            .OrderByDescending(c => c.RespondedAt)
            .ToListAsync();
    }

    public async Task<List<PlayerConnection>> GetPendingRequestsReceivedAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerConnections
            .Include(c => c.RequestingUser)
            .Where(c => c.TargetUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.RequestedAt)
            .ToListAsync();
    }

    public async Task<List<PlayerConnection>> GetPendingRequestsSentAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerConnections
            .Include(c => c.TargetUser)
            .Where(c => c.RequestingUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.RequestedAt)
            .ToListAsync();
    }

    public async Task<PlayerConnection?> GetConnectionAsync(string userId1, string userId2)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerConnections
            .Include(c => c.RequestingUser)
            .Include(c => c.TargetUser)
            .FirstOrDefaultAsync(c =>
                (c.RequestingUserId == userId1 && c.TargetUserId == userId2) ||
                (c.RequestingUserId == userId2 && c.TargetUserId == userId1));
    }

    public async Task<bool> AreConnectedAsync(string userId1, string userId2)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerConnections
            .AnyAsync(c =>
                ((c.RequestingUserId == userId1 && c.TargetUserId == userId2) ||
                 (c.RequestingUserId == userId2 && c.TargetUserId == userId1)) &&
                c.Status == ConnectionStatus.Accepted);
    }

    public async Task<List<UserSearchResult>> SearchUsersAsync(string searchTerm, string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return new List<UserSearchResult>();
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var normalizedSearch = searchTerm.ToLower();

        // Get users matching the search (excluding current user)
        var matchingUsers = await context.Users
            .Where(u => u.Id != currentUserId &&
                       (u.UserName != null && u.UserName.ToLower().Contains(normalizedSearch) ||
                        u.Email != null && u.Email.ToLower().Contains(normalizedSearch)))
            .Take(20)
            .ToListAsync();

        if (!matchingUsers.Any())
        {
            return new List<UserSearchResult>();
        }

        var userIds = matchingUsers.Select(u => u.Id).ToList();

        // Get existing connections for these users
        var existingConnections = await context.PlayerConnections
            .Where(c => (c.RequestingUserId == currentUserId && userIds.Contains(c.TargetUserId)) ||
                       (c.TargetUserId == currentUserId && userIds.Contains(c.RequestingUserId)))
            .ToListAsync();

        var results = new List<UserSearchResult>();
        foreach (var user in matchingUsers)
        {
            var connection = existingConnections.FirstOrDefault(c =>
                c.RequestingUserId == user.Id || c.TargetUserId == user.Id);

            results.Add(new UserSearchResult
            {
                UserId = user.Id,
                DisplayName = user.UserName ?? "Unknown",
                Email = user.Email,
                ExistingConnectionStatus = connection?.Status,
                IsRequestSentByCurrentUser = connection?.RequestingUserId == currentUserId
            });
        }

        return results;
    }
}
