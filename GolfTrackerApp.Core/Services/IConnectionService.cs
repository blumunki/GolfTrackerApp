using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IConnectionService
{
    // Connection requests
    Task<PlayerConnection> SendConnectionRequestAsync(string requestingUserId, string targetUserId);
    Task<PlayerConnection?> AcceptConnectionRequestAsync(int connectionId, string userId);
    Task<PlayerConnection?> DeclineConnectionRequestAsync(int connectionId, string userId);
    Task<bool> CancelConnectionRequestAsync(int connectionId, string userId);
    Task<bool> RemoveConnectionAsync(int connectionId, string userId);
    
    // Query connections
    Task<List<PlayerConnection>> GetConnectionsAsync(string userId);
    Task<List<PlayerConnection>> GetPendingRequestsReceivedAsync(string userId);
    Task<List<PlayerConnection>> GetPendingRequestsSentAsync(string userId);
    Task<PlayerConnection?> GetConnectionAsync(string userId1, string userId2);
    Task<bool> AreConnectedAsync(string userId1, string userId2);
    
    // Search for users to connect with
    Task<List<UserSearchResult>> SearchUsersAsync(string searchTerm, string currentUserId);
}

public class UserSearchResult
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public ConnectionStatus? ExistingConnectionStatus { get; set; }
    public bool IsRequestSentByCurrentUser { get; set; }
}
