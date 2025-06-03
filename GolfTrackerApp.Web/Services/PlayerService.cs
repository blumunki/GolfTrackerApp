// In GolfTrackerApp.Web/Services/PlayerService.cs
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(ApplicationDbContext context,ILogger<PlayerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // In GolfTrackerApp.Web/Services/PlayerService.cs
        public async Task<Player> AddPlayerAsync(Player player)
        {
            // Ensure CreatedByApplicationUserId is set if it's a managed player
            if (string.IsNullOrEmpty(player.ApplicationUserId) && string.IsNullOrEmpty(player.CreatedByApplicationUserId))
            {
                _logger.LogError("AddPlayerAsync: Managed player submitted without CreatedByApplicationUserId. FirstName: {FirstName}, LastName: {LastName}", player.FirstName, player.LastName);
                throw new InvalidOperationException("Managed players must have a CreatedByApplicationUserId.");
            }

            // If an ApplicationUserId is provided, check if it's already linked to a different Player profile.
            if (!string.IsNullOrEmpty(player.ApplicationUserId))
            {
                var existingPlayerForUser = await _context.Players
                                                .AsNoTracking() // Read-only check
                                                .FirstOrDefaultAsync(p => p.ApplicationUserId == player.ApplicationUserId);
                if (existingPlayerForUser != null)
                {
                    throw new InvalidOperationException($"A player profile (ID: {existingPlayerForUser.PlayerId}, Name: {existingPlayerForUser.FirstName} {existingPlayerForUser.LastName}) already exists for this system user account.");
                }
            }
            // Optional: Check for duplicate managed players by the same creator
            else if (!string.IsNullOrEmpty(player.CreatedByApplicationUserId))
            {
                var duplicateManagedPlayer = await _context.Players
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(p => string.IsNullOrEmpty(p.ApplicationUserId) && // is a managed player
                                                                    p.CreatedByApplicationUserId == player.CreatedByApplicationUserId &&
                                                                    p.FirstName == player.FirstName &&
                                                                    p.LastName == player.LastName);
                if (duplicateManagedPlayer != null)
                {
                    throw new InvalidOperationException($"You already manage a player named '{player.FirstName} {player.LastName}'.");
                }
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Player {PlayerId} created: {FirstName} {LastName}", player.PlayerId, player.FirstName, player.LastName);
            return player;
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                _logger.LogWarning("DeletePlayerAsync: Player with ID {PlayerId} not found for deletion.", id);
                return false;
            }
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Player {PlayerId} deleted: {FirstName} {LastName}", player.PlayerId, player.FirstName, player.LastName);
            return true;
        }

        // In GolfTrackerApp.Web/Services/PlayerService.cs
        public async Task<List<Player>> GetAllPlayersAsync(string requestingUserId, bool isUserAdmin)
        {
            IQueryable<Player> query = _context.Players.Include(p => p.ApplicationUser);

            if (!isUserAdmin)
            {
                // Regular users see:
                // 1. Their own player profile (if they are a registered player)
                // 2. Managed players they created
                // 3. (Optional - add this if desired) All other registered system players (public profiles)
                query = query.Where(p => (p.ApplicationUserId != null && p.ApplicationUserId == requestingUserId) || // Their own profile
                                        (p.ApplicationUserId == null && p.CreatedByApplicationUserId == requestingUserId) // Managed players they created
                                        // || (p.ApplicationUserId != null) // Uncomment to show all registered players to everyone
                                        );
            }
            // Admins see all players (no additional filtering needed beyond the base query)
            return await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
        }

        public async Task<Player?> GetPlayerByIdAsync(int id)
        {
            return await _context.Players.Include(p => p.ApplicationUser).FirstOrDefaultAsync(p => p.PlayerId == id);
        }

        public async Task<Player?> GetPlayerByApplicationUserIdAsync(string applicationUserId)
        {
             return await _context.Players.Include(p => p.ApplicationUser)
                                    .FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId);
        }

        public async Task<Player?> UpdatePlayerAsync(Player playerUpdateData) // playerUpdateData comes from the UI
        {
            var existingPlayer = await _context.Players
                                        .Include(p => p.ApplicationUser) // Include for checking ApplicationUserId changes
                                        .FirstOrDefaultAsync(p => p.PlayerId == playerUpdateData.PlayerId);
            if (existingPlayer == null)
            {
                _logger.LogWarning("UpdatePlayerAsync: Player with ID {PlayerId} not found.", playerUpdateData.PlayerId);
                return null;
            }

            // Explicitly prevent changes to CreatedByApplicationUserId
            if (existingPlayer.CreatedByApplicationUserId != playerUpdateData.CreatedByApplicationUserId &&
                !string.IsNullOrEmpty(playerUpdateData.CreatedByApplicationUserId)) // Allow if input is null/empty, but we'll enforce original
            {
                // Use _logger field
                _logger.LogWarning("Attempt to change CreatedByApplicationUserId for PlayerId {PlayerId} from {OriginalOwner} to {AttemptedOwner}. Change rejected.",
                    existingPlayer.PlayerId, existingPlayer.CreatedByApplicationUserId, playerUpdateData.CreatedByApplicationUserId);
                // The logic ensures CreatedByApplicationUserId from playerUpdateData is ignored, existingPlayer.CreatedByApplicationUserId is preserved.
            }
            // No need to set existingPlayer.CreatedByApplicationUserId = playerUpdateData.CreatedByApplicationUserId;
            // It simply won't be updated from playerUpdateData.

            // Handle changes to linkable ApplicationUserId (as in previous EditPlayer.razor logic)
            if (existingPlayer.ApplicationUserId != playerUpdateData.ApplicationUserId)
            {
                if (!string.IsNullOrEmpty(playerUpdateData.ApplicationUserId)) // If trying to link or change link
                {
                    var anotherPlayerWithUser = await _context.Players
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(p => p.PlayerId != playerUpdateData.PlayerId && 
                                                                        p.ApplicationUserId == playerUpdateData.ApplicationUserId);
                    if (anotherPlayerWithUser != null)
                    {
                        throw new InvalidOperationException($"The system user account is already linked to player '{anotherPlayerWithUser.FirstName} {anotherPlayerWithUser.LastName}'.");
                    }
                }
                existingPlayer.ApplicationUserId = playerUpdateData.ApplicationUserId; // Update the link
            }

            // Update other mutable fields
            existingPlayer.FirstName = playerUpdateData.FirstName;
            existingPlayer.LastName = playerUpdateData.LastName;
            existingPlayer.Handicap = playerUpdateData.Handicap;
            // Do NOT update existingPlayer.CreatedByApplicationUserId from playerUpdateData

            await _context.SaveChangesAsync();
            _logger.LogInformation("Player {PlayerId} updated: {FirstName} {LastName}", existingPlayer.PlayerId, existingPlayer.FirstName, existingPlayer.LastName);
            return existingPlayer;
        }
    }
}