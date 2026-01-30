using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IMergeService
{
    // Request a merge of managed player data to a connected user
    Task<PlayerMergeRequest> RequestMergeAsync(
        string requestingUserId, 
        int sourcePlayerId, 
        string targetUserId,
        string? message = null);
    
    // Accept a merge request (transfers data)
    Task<PlayerMergeRequest?> AcceptMergeRequestAsync(int mergeRequestId, string userId);
    
    // Decline a merge request
    Task<PlayerMergeRequest?> DeclineMergeRequestAsync(int mergeRequestId, string userId);
    
    // Cancel a pending merge request (by requester)
    Task<bool> CancelMergeRequestAsync(int mergeRequestId, string userId);
    
    // Get pending merge requests received by a user
    Task<List<PlayerMergeRequest>> GetPendingMergeRequestsReceivedAsync(string userId);
    
    // Get pending merge requests sent by a user
    Task<List<PlayerMergeRequest>> GetPendingMergeRequestsSentAsync(string userId);
    
    // Get managed players that can be merged to a specific connected user
    Task<List<Player>> GetMergeablePlayers(string currentUserId, string targetUserId);
    
    // Check if a player already has a pending merge request
    Task<bool> HasPendingMergeRequest(int sourcePlayerId);
}
