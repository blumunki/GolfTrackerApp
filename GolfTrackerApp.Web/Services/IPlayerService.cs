// In GolfTrackerApp.Web/Services/IPlayerService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IPlayerService
    {
        Task<List<Player>> GetAllPlayersAsync();
        Task<Player?> GetPlayerByIdAsync(int id);
        Task<Player?> GetPlayerByApplicationUserIdAsync(string applicationUserId);
        Task<Player> AddPlayerAsync(Player player);
        Task<Player?> UpdatePlayerAsync(Player player);
        Task<bool> DeletePlayerAsync(int id);
        // We might need a method to create a Player profile when a new user registers.
        // Task<Player> CreatePlayerForNewUserAsync(string applicationUserId, string firstName, string lastName);
    }
}