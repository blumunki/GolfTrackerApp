using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Core.Services
{
    public class ScoreService : IScoreService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ScoreService> _logger;
        private readonly IHandicapService _handicapService;

        public ScoreService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<ScoreService> logger,
            IHandicapService handicapService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _handicapService = handicapService;
        }

        public async Task<Score> AddScoreAsync(Score score)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // Add validation for PlayerId, RoundId, HoleId if needed
            _context.Scores.Add(score);
            await _context.SaveChangesAsync();
            return score;
        }

        public async Task<List<Score>> AddScoresAsync(IEnumerable<Score> scores)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            _context.Scores.AddRange(scores);
            await _context.SaveChangesAsync();
            return scores.ToList();
        }

        public async Task<bool> DeleteScoreAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var score = await _context.Scores.FindAsync(id);
            if (score == null) return false;
            _context.Scores.Remove(score);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Score?> GetScoreByIdAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Scores
                                 .Include(s => s.Player)
                                 .Include(s => s.Round)
                                 .Include(s => s.Hole)
                                 .FirstOrDefaultAsync(s => s.ScoreId == id);
        }

        public async Task<List<Score>> GetScoresForPlayerInRoundAsync(int roundId, int playerId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Scores
                                 .Where(s => s.RoundId == roundId && s.PlayerId == playerId)
                                 .Include(s => s.Hole) // So you know which hole the score is for
                                 .OrderBy(s => s.Hole!.HoleNumber) // Order by hole number
                                 .ToListAsync();
        }

        public async Task<List<Score>> GetScoresForRoundAsync(int roundId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Scores
                                 .Where(s => s.RoundId == roundId)
                                 .Include(s => s.Player)
                                 .Include(s => s.Hole)
                                 .OrderBy(s => s.PlayerId).ThenBy(s => s.Hole!.HoleNumber)
                                 .ToListAsync();
        }

        public async Task<Score?> UpdateScoreAsync(Score score)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var existingScore = await _context.Scores.FindAsync(score.ScoreId);
            if (existingScore == null) return null;
            // Add validation if critical FKs are being changed
            _context.Entry(existingScore).CurrentValues.SetValues(score);
            await _context.SaveChangesAsync();
            return existingScore;
        }
        public async Task SaveScorecardAsync(int roundId, Dictionary<int, List<HoleScoreEntryModel>> scorecard)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
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
                            FairwayHit = holeScore.FairwayHit,
                            TeeSetId = holeScore.TeeSetId
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

            // This path completes rounds directly (and re-saves edited scorecards), so
            // recalculate handicaps here too. The recalculation is idempotent; a failure
            // must not fail the scorecard save.
            await RecalculateHandicapsAsync(roundId);
        }

        public async Task<int> UpdateRoundScoresAsync(int roundId, IEnumerable<ScoreUpdateDto> updates)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var existingScores = await _context.Scores
                .Where(s => s.RoundId == roundId)
                .ToListAsync();

            var updated = 0;
            foreach (var update in updates)
            {
                var score = existingScores.FirstOrDefault(s => s.ScoreId == update.ScoreId);
                if (score is null) continue; // not part of this round — ignore

                score.Strokes = update.Strokes;
                score.Putts = update.Putts;
                score.FairwayHit = update.FairwayHit;
                updated++;
            }

            await _context.SaveChangesAsync();

            if (updated > 0)
            {
                await RecalculateHandicapsAsync(roundId);
            }
            return updated;
        }

        /// <summary>Idempotent handicap recalc; a failure must not fail the score save.</summary>
        private async Task RecalculateHandicapsAsync(int roundId)
        {
            try
            {
                await _handicapService.OnRoundCompletedAsync(roundId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handicap recalculation failed for Round {RoundId}.", roundId);
            }
        }
        // Additional methods can be implemented as needed
    }
}
