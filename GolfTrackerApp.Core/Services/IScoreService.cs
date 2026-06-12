// In GolfTrackerApp.Web/Services/IScoreService.cs
using GolfTrackerApp.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Core.Services
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
        Task SaveScorecardAsync(int roundId, Dictionary<int, List<HoleScoreEntryModel>> scorecard);

        /// <summary>
        /// Applies stroke/putt/fairway edits to existing scores of one round (updates whose
        /// ScoreId does not belong to the round are ignored), then recalculates handicaps.
        /// Returns the number of scores updated.
        /// </summary>
        Task<int> UpdateRoundScoresAsync(int roundId, IEnumerable<ScoreUpdateDto> updates);
    }
}