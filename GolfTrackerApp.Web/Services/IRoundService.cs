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
        Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds, Dictionary<int, int?>? teeSelections);
        Task<Round?> UpdateRoundAsync(Round round, IEnumerable<int>? playerIdsToUpdate = null);
        Task<bool> DeleteRoundAsync(int id);
        // GetRoundsForPlayerAsync might implicitly use requestingUserId if it's different from playerId parameter
        Task<List<Round>> GetRoundsForPlayerAsync(int playerId, string requestingUserId, bool isUserAdmin);
        // In IRoundService.cs
        Task<Round> CreateRoundWithPlayersAsync(Round round, List<int> playerIds);
        Task<List<Round>> GetRecentRoundsAsync(string requestingUserId, bool isUserAdmin, int count);
        Task<List<Round>> SearchRoundsAsync(string requestingUserId, bool isUserAdmin, string searchTerm); 
        Task<Scorecard> PrepareScorecardAsync(int courseId, int startingHole, int holesPlayed, List<Player> players);
        Task<Scorecard> PrepareScorecardAsync(int courseId, int startingHole, int holesPlayed, List<Player> players, Dictionary<int, int?>? teeSelections);

        // New methods for club/course specific recent rounds
        Task<List<Round>> GetRecentRoundsForClubAsync(string requestingUserId, int clubId, int count);
        Task<List<Round>> GetRecentRoundsForCourseAsync(string requestingUserId, int courseId, int count);

        // Count methods for total rounds played at a club/course
        Task<int> GetRoundCountForClubAsync(string requestingUserId, int clubId);
        Task<int> GetRoundCountForCourseAsync(string requestingUserId, int courseId);
    }
}