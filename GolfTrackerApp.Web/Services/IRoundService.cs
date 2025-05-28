// In GolfTrackerApp.Web/Services/IRoundService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IRoundService
    {
        Task<List<Round>> GetAllRoundsAsync(); // Might need pagination later
        Task<Round?> GetRoundByIdAsync(int id);
        Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds);
        Task<Round?> UpdateRoundAsync(Round round, IEnumerable<int>? playerIdsToUpdate = null); // Optional player update
        Task<bool> DeleteRoundAsync(int id);
        Task<List<Round>> GetRoundsForPlayerAsync(int playerId);
    }
}