// In GolfTrackerApp.Web/Services/IRoundService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IRoundService
    {
        // Add requestingUserId and isUserAdmin parameters
        Task<List<Round>> GetAllRoundsAsync(string requestingUserId, bool isUserAdmin);
        Task<Round?> GetRoundByIdAsync(int id); // Potentially add user context for authorization later
        // AddRoundAsync will take the Round object which should have CreatedByApplicationUserId pre-filled
        Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds);
        Task<Round?> UpdateRoundAsync(Round round, IEnumerable<int>? playerIdsToUpdate = null);
        Task<bool> DeleteRoundAsync(int id);
        // GetRoundsForPlayerAsync might implicitly use requestingUserId if it's different from playerId parameter
        Task<List<Round>> GetRoundsForPlayerAsync(int playerId, string requestingUserId, bool isUserAdmin);
        // In IRoundService.cs
        Task<Round> CreateRoundWithPlayersAsync(Round round, List<int> playerIds);
        Task<List<Round>> GetRecentRoundsAsync(string requestingUserId, bool isUserAdmin, int count);
    }
}