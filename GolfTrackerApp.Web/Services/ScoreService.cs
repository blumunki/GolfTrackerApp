// In GolfTrackerApp.Web/Services/ScoreService.cs
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class ScoreService : IScoreService
    {
        private readonly ApplicationDbContext _context;

        public ScoreService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Score> AddScoreAsync(Score score)
        {
            // Add validation for PlayerId, RoundId, HoleId if needed
            _context.Scores.Add(score);
            await _context.SaveChangesAsync();
            return score;
        }

        public async Task<List<Score>> AddScoresAsync(IEnumerable<Score> scores)
        {
            _context.Scores.AddRange(scores);
            await _context.SaveChangesAsync();
            return scores.ToList();
        }

        public async Task<bool> DeleteScoreAsync(int id)
        {
            var score = await _context.Scores.FindAsync(id);
            if (score == null) return false;
            _context.Scores.Remove(score);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Score?> GetScoreByIdAsync(int id)
        {
            return await _context.Scores
                                 .Include(s => s.Player)
                                 .Include(s => s.Round)
                                 .Include(s => s.Hole)
                                 .FirstOrDefaultAsync(s => s.ScoreId == id);
        }

        public async Task<List<Score>> GetScoresForPlayerInRoundAsync(int roundId, int playerId)
        {
            return await _context.Scores
                                 .Where(s => s.RoundId == roundId && s.PlayerId == playerId)
                                 .Include(s => s.Hole) // So you know which hole the score is for
                                 .OrderBy(s => s.Hole!.HoleNumber) // Order by hole number
                                 .ToListAsync();
        }

        public async Task<List<Score>> GetScoresForRoundAsync(int roundId)
        {
            return await _context.Scores
                                 .Where(s => s.RoundId == roundId)
                                 .Include(s => s.Player)
                                 .Include(s => s.Hole)
                                 .OrderBy(s => s.PlayerId).ThenBy(s => s.Hole!.HoleNumber)
                                 .ToListAsync();
        }

        public async Task<Score?> UpdateScoreAsync(Score score)
        {
            var existingScore = await _context.Scores.FindAsync(score.ScoreId);
            if (existingScore == null) return null;
            // Add validation if critical FKs are being changed
            _context.Entry(existingScore).CurrentValues.SetValues(score);
            await _context.SaveChangesAsync();
            return existingScore;
        }
        public async Task SaveScorecardAsync(int roundId, Dictionary<int, List<HoleScoreEntryModel>> scorecard)
        {
            // First, remove any existing scores for this round to handle edits
            var existingScores = _context.Scores.Where(s => s.RoundId == roundId);
            _context.Scores.RemoveRange(existingScores);

            // Create new score entities from the scorecard model
            var newScores = new List<Score>();
            foreach (var playerScores in scorecard)
            {
                var playerId = playerScores.Key;
                foreach (var holeScore in playerScores.Value)
                {
                    if (holeScore.Strokes.HasValue) // Only save scores that have been entered
                    {
                        newScores.Add(new Score
                        {
                            RoundId = roundId,
                            PlayerId = playerId,
                            HoleId = holeScore.HoleId,
                            Strokes = holeScore.Strokes.Value,
                            Putts = holeScore.Putts,
                            FairwayHit = holeScore.FairwayHit
                        });
                    }
                }
            }

            await _context.Scores.AddRangeAsync(newScores);

            // Finally, update the Round's status to Completed
            var round = await _context.Rounds.FindAsync(roundId);
            if (round != null)
            {
                round.Status = RoundCompletionStatus.Completed;
            }

            await _context.SaveChangesAsync();
        }
        // Additional methods can be implemented as needed
    }
}