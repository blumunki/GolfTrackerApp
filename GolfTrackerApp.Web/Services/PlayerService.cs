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

        public async Task<Player> AddPlayerAsync(Player player)
        {
            // If an ApplicationUserId is provided, check if it's already linked.
            if (!string.IsNullOrEmpty(player.ApplicationUserId))
            {
                var existingPlayerForUser = await _context.Players
                                                .FirstOrDefaultAsync(p => p.ApplicationUserId == player.ApplicationUserId);
                if (existingPlayerForUser != null)
                {
                    throw new InvalidOperationException($"A player profile (ID: {existingPlayerForUser.PlayerId}) already exists for this application user ID.");
                }
            }
            else // This is a managed player (ApplicationUserId is null or empty)
            {
                // Optional: Add logic to prevent duplicate managed players by FirstName/LastName if desired.
                // For example:
                // var existingManagedPlayer = await _context.Players
                //    .FirstOrDefaultAsync(p => p.ApplicationUserId == null &&
                //                              p.FirstName == player.FirstName &&
                //                              p.LastName == player.LastName);
                // if (existingManagedPlayer != null)
                // {
                //    throw new InvalidOperationException($"A managed player named '{player.FirstName} {player.LastName}' already exists.");
                // }
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

        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _context.Players.Include(p => p.ApplicationUser).ToListAsync(); // Include user details
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