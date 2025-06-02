// In GolfTrackerApp.Web/Services/IPlayerService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IPlayerService
    {
        // Add requestingUserId and isUserAdmin parameters
        Task<List<Player>> GetAllPlayersAsync(string requestingUserId, bool isUserAdmin);
        Task<Player?> GetPlayerByIdAsync(int id); // Might also need context if editing others' managed players
        Task<Player?> GetPlayerByApplicationUserIdAsync(string applicationUserId);
        // AddPlayerAsync will take the Player object which should have CreatedByApplicationUserId pre-filled by calling code for managed players
        Task<Player> AddPlayerAsync(Player player);
        Task<Player?> UpdatePlayerAsync(Player player); // Needs context for authorization
        Task<bool> DeletePlayerAsync(int id); // Needs context for authorization
    }
}