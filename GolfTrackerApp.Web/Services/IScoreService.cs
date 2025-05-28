// In GolfTrackerApp.Web/Services/IScoreService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IScoreService
    {
        Task<List<Score>> GetScoresForRoundAsync(int roundId);
        Task<List<Score>> GetScoresForPlayerInRoundAsync(int roundId, int playerId);
        Task<Score?> GetScoreByIdAsync(int id);
        Task<Score> AddScoreAsync(Score score);
        Task<List<Score>> AddScoresAsync(IEnumerable<Score> scores); // For bulk
        Task<Score?> UpdateScoreAsync(Score score);
        Task<bool> DeleteScoreAsync(int id);
    }
}