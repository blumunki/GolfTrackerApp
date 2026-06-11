// In GolfTrackerApp.Web/Services/IPlayerService.cs
using GolfTrackerApp.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Core.Services
{
    public interface IPlayerService
    {
        Task<List<Player>> GetAllPlayersAsync(string requestingUserId, bool isUserAdmin);
        Task<List<Player>> SearchPlayersAsync(string requestingUserId, bool isUserAdmin, string searchTerm);
        Task<Player?> GetPlayerByIdAsync(int id);
        Task<Player?> GetPlayerByApplicationUserIdAsync(string applicationUserId);
        Task<Player> AddPlayerAsync(Player player);
        Task<Player?> UpdatePlayerAsync(Player player);
        Task<bool> DeletePlayerAsync(int id);
    }
}