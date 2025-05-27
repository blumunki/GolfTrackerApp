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
            // Basic validation: ensure ApplicationUserId is not already linked to another Player
            var existingPlayerForUser = await _context.Players
                                            .FirstOrDefaultAsync(p => p.ApplicationUserId == player.ApplicationUserId);
            if (existingPlayerForUser != null)
            {
                throw new InvalidOperationException("A player profile already exists for this application user.");
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
            // Prevent changing the ApplicationUserId for an existing player profile
            if (existingPlayer.ApplicationUserId != player.ApplicationUserId)
            {
               throw new InvalidOperationException("Cannot change the ApplicationUserId of an existing player profile.");
            }

            _context.Entry(existingPlayer).CurrentValues.SetValues(player);
            // Ensure ApplicationUserId is not overwritten by SetValues if it's not part of the editable fields
            existingPlayer.ApplicationUserId = player.ApplicationUserId;


            await _context.SaveChangesAsync();
            return existingPlayer;
        }
    }
}