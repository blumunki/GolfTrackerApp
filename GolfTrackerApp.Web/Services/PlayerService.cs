// In GolfTrackerApp.Web/Services/PlayerService.cs
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly ApplicationDbContext _context;

        public PlayerService(ApplicationDbContext context)
        {
            _context = context;
        }

        // In GolfTrackerApp.Web/Services/PlayerService.cs
        public async Task<Player> AddPlayerAsync(Player player)
        {
            // Ensure CreatedByApplicationUserId is set if it's a managed player
            if (string.IsNullOrEmpty(player.ApplicationUserId) && string.IsNullOrEmpty(player.CreatedByApplicationUserId))
            {
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
            return player;
        }

        public async Task<bool> DeletePlayerAsync(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return false;
            }
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
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

        public async Task<Player?> UpdatePlayerAsync(Player player)
        {
            var existingPlayer = await _context.Players.FindAsync(player.PlayerId);
            if (existingPlayer == null)
            {
                return null;
            }

            // If ApplicationUserId is being set or changed, ensure it's not already in use by another player profile
            if (!string.IsNullOrEmpty(player.ApplicationUserId) && existingPlayer.ApplicationUserId != player.ApplicationUserId)
            {
                var anotherPlayerWithUser = await _context.Players
                                                .FirstOrDefaultAsync(p => p.PlayerId != player.PlayerId && p.ApplicationUserId == player.ApplicationUserId);
                if (anotherPlayerWithUser != null)
                {
                    throw new InvalidOperationException($"The ApplicationUser ID '{player.ApplicationUserId}' is already linked to another player profile (ID: {anotherPlayerWithUser.PlayerId}).");
                }
            }

            _context.Entry(existingPlayer).CurrentValues.SetValues(player);
            // Explicitly set ApplicationUserId as SetValues might not handle transitions from value to null well if not careful.
            existingPlayer.ApplicationUserId = player.ApplicationUserId;


            await _context.SaveChangesAsync();
            return existingPlayer;
        }
    }
}