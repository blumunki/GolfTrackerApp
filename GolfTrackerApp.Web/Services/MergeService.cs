using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class MergeService : IMergeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MergeService> _logger;

    public MergeService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        INotificationService notificationService,
        ILogger<MergeService> logger)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PlayerMergeRequest> RequestMergeAsync(
        string requestingUserId,
        int sourcePlayerId,
        string targetUserId,
        string? message = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Validate: source player must be managed by requesting user
        var sourcePlayer = await context.Players
            .FirstOrDefaultAsync(p => p.PlayerId == sourcePlayerId && p.CreatedByApplicationUserId == requestingUserId);

        if (sourcePlayer == null)
        {
            throw new InvalidOperationException("Source player not found or you don't have permission.");
        }

        // Validate: source player must not already have an ApplicationUserId (must be managed)
        if (!string.IsNullOrEmpty(sourcePlayer.ApplicationUserId))
        {
            throw new InvalidOperationException("Cannot merge a player that is already linked to a user account.");
        }

        // Validate: target user must have a player profile
        var targetPlayer = await context.Players
            .FirstOrDefaultAsync(p => p.ApplicationUserId == targetUserId);

        if (targetPlayer == null)
        {
            // Get target user's name for better error message
            var targetUser = await context.Users.FindAsync(targetUserId);
            var targetName = targetUser?.UserName ?? "The target user";
            throw new InvalidOperationException($"{targetName} needs to create their player profile first before they can receive data transfers. They can do this on the Players page by clicking 'Add New Player' and selecting themselves as the registered user.");
        }

        // Validate: no existing pending merge for this source player
        var existingPending = await context.PlayerMergeRequests
            .AnyAsync(m => m.SourcePlayerId == sourcePlayerId && m.Status == MergeRequestStatus.Pending);

        if (existingPending)
        {
            throw new InvalidOperationException("There is already a pending merge request for this player.");
        }

        // Validate: users must be connected
        var areConnected = await context.PlayerConnections
            .AnyAsync(c =>
                ((c.RequestingUserId == requestingUserId && c.TargetUserId == targetUserId) ||
                 (c.RequestingUserId == targetUserId && c.TargetUserId == requestingUserId)) &&
                c.Status == ConnectionStatus.Accepted);

        if (!areConnected)
        {
            throw new InvalidOperationException("You must be connected with this user to request a data merge.");
        }

        var mergeRequest = new PlayerMergeRequest
        {
            RequestingUserId = requestingUserId,
            TargetUserId = targetUserId,
            SourcePlayerId = sourcePlayerId,
            TargetPlayerId = targetPlayer.PlayerId,
            Status = MergeRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            Message = message
        };

        context.PlayerMergeRequests.Add(mergeRequest);
        await context.SaveChangesAsync();

        // Notify target user
        var requester = await context.Users.FindAsync(requestingUserId);
        var requesterName = requester?.UserName ?? "Someone";

        await _notificationService.CreateMergeRequestNotificationAsync(
            targetUserId, requesterName, $"{sourcePlayer.FirstName} {sourcePlayer.LastName}", mergeRequest.Id);

        _logger.LogInformation("Merge request {MergeId} created: Player {SourceId} -> {TargetId}",
            mergeRequest.Id, sourcePlayerId, targetPlayer.PlayerId);

        return mergeRequest;
    }

    public async Task<PlayerMergeRequest?> AcceptMergeRequestAsync(int mergeRequestId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var mergeRequest = await context.PlayerMergeRequests
            .Include(m => m.SourcePlayer)
            .Include(m => m.TargetPlayer)
            .Include(m => m.RequestingUser)
            .Include(m => m.TargetUser)
            .FirstOrDefaultAsync(m => m.Id == mergeRequestId && m.TargetUserId == userId);

        if (mergeRequest == null || mergeRequest.Status != MergeRequestStatus.Pending)
        {
            return null;
        }

        // Perform the merge - transfer rounds from source player to target player
        var (roundsMerged, roundsSkipped) = await MergePlayerDataAsync(
            context, mergeRequest.SourcePlayerId, mergeRequest.TargetPlayerId);

        mergeRequest.Status = MergeRequestStatus.Accepted;
        mergeRequest.CompletedAt = DateTime.UtcNow;
        mergeRequest.RoundsMerged = roundsMerged;
        mergeRequest.RoundsSkipped = roundsSkipped;

        await context.SaveChangesAsync();

        // Delete the obsolete source player (managed player that has been merged)
        await DeleteObsoleteSourcePlayerAsync(context, mergeRequest.SourcePlayerId);

        // Notify the requester
        var accepterName = mergeRequest.TargetUser?.UserName ?? "Someone";
        await _notificationService.CreateMergeCompletedNotificationAsync(
            mergeRequest.RequestingUserId, accepterName, roundsMerged, roundsSkipped, mergeRequest.Id);

        _logger.LogInformation("Merge request {MergeId} accepted. {Merged} rounds merged, {Skipped} skipped.",
            mergeRequestId, roundsMerged, roundsSkipped);

        return mergeRequest;
    }

    private async Task<(int roundsMerged, int roundsSkipped)> MergePlayerDataAsync(
        ApplicationDbContext context, int sourcePlayerId, int targetPlayerId)
    {
        int roundsMerged = 0;
        int roundsSkipped = 0;

        // Use explicit transaction for safety
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Get all scores for source player, grouped by round
            var sourceScores = await context.Scores
                .Include(s => s.Round)
                .Where(s => s.PlayerId == sourcePlayerId)
                .ToListAsync();

            // Get rounds where target player already has scores (to detect duplicates)
            var targetExistingRoundIds = await context.Scores
                .Where(s => s.PlayerId == targetPlayerId)
                .Select(s => s.RoundId)
                .Distinct()
                .ToListAsync();

            var roundsToProcess = sourceScores
                .GroupBy(s => s.RoundId)
                .ToList();

            // Identify rounds to merge (excluding duplicates)
            var affectedRoundIds = roundsToProcess
                .Where(g => !targetExistingRoundIds.Contains(g.Key))
                .Select(g => g.Key)
                .ToList();

            // Check for rounds that would be skipped
            foreach (var roundGroup in roundsToProcess)
            {
                if (targetExistingRoundIds.Contains(roundGroup.Key))
                {
                    roundsSkipped++;
                }
            }

            // PHASE 1: INSERT new RoundPlayer records for target player FIRST
            // Check which RoundPlayer entries need to be created
            var existingTargetRoundPlayerIds = await context.Set<RoundPlayer>()
                .Where(rp => affectedRoundIds.Contains(rp.RoundId) && rp.PlayerId == targetPlayerId)
                .Select(rp => rp.RoundId)
                .ToListAsync();

            var roundIdsNeedingNewRoundPlayer = affectedRoundIds
                .Where(rid => !existingTargetRoundPlayerIds.Contains(rid))
                .ToList();

            // Create new RoundPlayer entries for target player
            var newRoundPlayers = roundIdsNeedingNewRoundPlayer
                .Select(roundId => new RoundPlayer { RoundId = roundId, PlayerId = targetPlayerId })
                .ToList();

            if (newRoundPlayers.Any())
            {
                context.Set<RoundPlayer>().AddRange(newRoundPlayers);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Phase 1: Created {Count} new RoundPlayer records for target player {PlayerId}",
                    newRoundPlayers.Count, targetPlayerId);
            }

            // PHASE 2: VALIDATE - Verify target player now has RoundPlayer entries for all affected rounds
            var targetRoundPlayerCount = await context.Set<RoundPlayer>()
                .CountAsync(rp => affectedRoundIds.Contains(rp.RoundId) && rp.PlayerId == targetPlayerId);

            if (targetRoundPlayerCount != affectedRoundIds.Count)
            {
                throw new InvalidOperationException(
                    $"Validation failed: Expected {affectedRoundIds.Count} RoundPlayer records for target, found {targetRoundPlayerCount}. Rolling back.");
            }

            _logger.LogInformation("Phase 2: Validated {Count} RoundPlayer records exist for target player",
                targetRoundPlayerCount);

            // PHASE 3: Transfer scores to target player
            foreach (var roundGroup in roundsToProcess)
            {
                if (targetExistingRoundIds.Contains(roundGroup.Key))
                {
                    continue; // Skip duplicates
                }

                foreach (var score in roundGroup)
                {
                    score.PlayerId = targetPlayerId;
                }

                roundsMerged++;
            }

            await context.SaveChangesAsync();
            
            _logger.LogInformation("Phase 3: Transferred scores for {Count} rounds to target player",
                roundsMerged);

            // PHASE 4: VALIDATE scores were transferred correctly
            var transferredScoreCount = await context.Scores
                .CountAsync(s => affectedRoundIds.Contains(s.RoundId) && s.PlayerId == targetPlayerId);
            
            var expectedScoreCount = sourceScores
                .Where(s => affectedRoundIds.Contains(s.RoundId))
                .Count();

            if (transferredScoreCount < expectedScoreCount)
            {
                throw new InvalidOperationException(
                    $"Validation failed: Expected at least {expectedScoreCount} scores for target, found {transferredScoreCount}. Rolling back.");
            }

            _logger.LogInformation("Phase 4: Validated {Count} scores now belong to target player",
                transferredScoreCount);

            // PHASE 5: DELETE old RoundPlayer records for source player (only after validation)
            var sourceRoundPlayersToRemove = await context.Set<RoundPlayer>()
                .Where(rp => affectedRoundIds.Contains(rp.RoundId) && rp.PlayerId == sourcePlayerId)
                .ToListAsync();

            if (sourceRoundPlayersToRemove.Any())
            {
                context.Set<RoundPlayer>().RemoveRange(sourceRoundPlayersToRemove);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Phase 5: Removed {Count} old RoundPlayer records for source player {PlayerId}",
                    sourceRoundPlayersToRemove.Count, sourcePlayerId);
            }

            // PHASE 6: Final validation - ensure we haven't lost any data
            var finalTargetRoundPlayerCount = await context.Set<RoundPlayer>()
                .CountAsync(rp => affectedRoundIds.Contains(rp.RoundId) && rp.PlayerId == targetPlayerId);

            if (finalTargetRoundPlayerCount != affectedRoundIds.Count)
            {
                throw new InvalidOperationException(
                    $"Final validation failed: Expected {affectedRoundIds.Count} RoundPlayer records, found {finalTargetRoundPlayerCount}. Rolling back.");
            }

            // All validations passed - commit the transaction
            await transaction.CommitAsync();
            
            _logger.LogInformation("Merge completed successfully. {Merged} rounds merged, {Skipped} skipped.",
                roundsMerged, roundsSkipped);

            return (roundsMerged, roundsSkipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge failed, rolling back transaction");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PlayerMergeRequest?> DeclineMergeRequestAsync(int mergeRequestId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var mergeRequest = await context.PlayerMergeRequests
            .FirstOrDefaultAsync(m => m.Id == mergeRequestId && m.TargetUserId == userId);

        if (mergeRequest == null || mergeRequest.Status != MergeRequestStatus.Pending)
        {
            return null;
        }

        mergeRequest.Status = MergeRequestStatus.Declined;
        mergeRequest.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Merge request {MergeId} declined by {UserId}", mergeRequestId, userId);

        return mergeRequest;
    }

    public async Task<bool> CancelMergeRequestAsync(int mergeRequestId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var mergeRequest = await context.PlayerMergeRequests
            .FirstOrDefaultAsync(m => m.Id == mergeRequestId &&
                                      m.RequestingUserId == userId &&
                                      m.Status == MergeRequestStatus.Pending);

        if (mergeRequest == null)
        {
            return false;
        }

        mergeRequest.Status = MergeRequestStatus.Cancelled;
        mergeRequest.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _logger.LogInformation("Merge request {MergeId} cancelled by {UserId}", mergeRequestId, userId);

        return true;
    }

    public async Task<List<PlayerMergeRequest>> GetPendingMergeRequestsReceivedAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerMergeRequests
            .Include(m => m.RequestingUser)
            .Include(m => m.SourcePlayer)
            .Where(m => m.TargetUserId == userId && m.Status == MergeRequestStatus.Pending)
            .OrderByDescending(m => m.RequestedAt)
            .ToListAsync();
    }

    public async Task<List<PlayerMergeRequest>> GetPendingMergeRequestsSentAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerMergeRequests
            .Include(m => m.TargetUser)
            .Include(m => m.SourcePlayer)
            .Where(m => m.RequestingUserId == userId && m.Status == MergeRequestStatus.Pending)
            .OrderByDescending(m => m.RequestedAt)
            .ToListAsync();
    }

    public async Task<List<Player>> GetMergeablePlayers(string currentUserId, string targetUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Get managed players (no ApplicationUserId) created by current user
        // that don't already have a pending merge request
        var pendingMergePlayerIds = await context.PlayerMergeRequests
            .Where(m => m.Status == MergeRequestStatus.Pending)
            .Select(m => m.SourcePlayerId)
            .ToListAsync();

        return await context.Players
            .Where(p => p.CreatedByApplicationUserId == currentUserId &&
                       string.IsNullOrEmpty(p.ApplicationUserId) &&
                       !pendingMergePlayerIds.Contains(p.PlayerId))
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();
    }

    public async Task<bool> HasPendingMergeRequest(int sourcePlayerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlayerMergeRequests
            .AnyAsync(m => m.SourcePlayerId == sourcePlayerId && m.Status == MergeRequestStatus.Pending);
    }

    /// <summary>
    /// Deletes the obsolete source player after a successful merge.
    /// This is the managed player that has been fully transferred to a registered user.
    /// </summary>
    private async Task DeleteObsoleteSourcePlayerAsync(ApplicationDbContext context, int sourcePlayerId)
    {
        var sourcePlayer = await context.Players.FindAsync(sourcePlayerId);
        if (sourcePlayer != null)
        {
            // Verify the player has no remaining scores (all should have been transferred)
            var remainingScores = await context.Scores.AnyAsync(s => s.PlayerId == sourcePlayerId);
            if (remainingScores)
            {
                _logger.LogWarning("Source player {PlayerId} still has scores after merge - not deleting", sourcePlayerId);
                return;
            }

            // Verify the player has no remaining round participations
            var remainingRoundPlayers = await context.Set<RoundPlayer>().AnyAsync(rp => rp.PlayerId == sourcePlayerId);
            if (remainingRoundPlayers)
            {
                _logger.LogWarning("Source player {PlayerId} still has round participations after merge - not deleting", sourcePlayerId);
                return;
            }

            // Delete related merge requests that reference this player as the source
            var relatedMergeRequests = await context.PlayerMergeRequests
                .Where(m => m.SourcePlayerId == sourcePlayerId)
                .ToListAsync();
            if (relatedMergeRequests.Any())
            {
                context.PlayerMergeRequests.RemoveRange(relatedMergeRequests);
                _logger.LogInformation("Deleted {Count} merge requests referencing player {PlayerId}", relatedMergeRequests.Count, sourcePlayerId);
            }

            context.Players.Remove(sourcePlayer);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted obsolete source player {PlayerId} after successful merge", sourcePlayerId);
        }
    }
}
