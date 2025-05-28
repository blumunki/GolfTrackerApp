// In GolfTrackerApp.Web/Services/RoundService.cs
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class RoundService : IRoundService
    {
        private readonly ApplicationDbContext _context;

        public RoundService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds)
        {
            if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == round.GolfCourseId))
            {
                throw new ArgumentException($"GolfCourse with ID {round.GolfCourseId} does not exist.");
            }
            if (playerIds == null || !playerIds.Any())
            {
                throw new ArgumentException("At least one player must be selected for the round.");
            }

            _context.Rounds.Add(round); // EF will assign RoundId after SaveChangesAsync

            // Need to save round first to get its ID for RoundPlayers if round.RoundId is 0
            await _context.SaveChangesAsync();

            foreach (var playerId in playerIds)
            {
                if (!await _context.Players.AnyAsync(p => p.PlayerId == playerId))
                {
                    // Handle or log error: Player not found
                    continue; // Or throw exception based on desired strictness
                }
                round.RoundPlayers.Add(new RoundPlayer { RoundId = round.RoundId, PlayerId = playerId });
            }
            // If round.RoundPlayers was populated before Add, EF might handle this.
            // If adding after round is tracked, ensure changes are saved.
            await _context.SaveChangesAsync();
            return round;
        }


        public async Task<bool> DeleteRoundAsync(int id)
        {
            var round = await _context.Rounds.Include(r => r.RoundPlayers).Include(r => r.Scores).FirstOrDefaultAsync(r => r.RoundId == id);
            if (round == null) return false;

            _context.Scores.RemoveRange(round.Scores); // Remove dependent scores
            _context.RoundPlayers.RemoveRange(round.RoundPlayers); // Remove join table entries
            _context.Rounds.Remove(round);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Round>> GetAllRoundsAsync()
        {
            return await _context.Rounds
                                 .Include(r => r.GolfCourse)
                                 .Include(r => r.RoundPlayers)
                                   .ThenInclude(rp => rp.Player)
                                 .OrderByDescending(r => r.DatePlayed)
                                 .ToListAsync();
        }

        public async Task<Round?> GetRoundByIdAsync(int id)
        {
            return await _context.Rounds
                                 .Include(r => r.GolfCourse)
                                 .Include(r => r.Scores)
                                   .ThenInclude(s => s.Hole)
                                 .Include(r => r.RoundPlayers)
                                   .ThenInclude(rp => rp.Player)
                                 .FirstOrDefaultAsync(r => r.RoundId == id);
        }

        public async Task<List<Round>> GetRoundsForPlayerAsync(int playerId)
        {
            return await _context.Rounds
                .Include(r => r.GolfCourse)
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId))
                .OrderByDescending(r => r.DatePlayed)
                .ToListAsync();
        }

        public async Task<Round?> UpdateRoundAsync(Round round, IEnumerable<int>? playerIdsToUpdate = null)
        {
            var existingRound = await _context.Rounds
                                              .Include(r => r.RoundPlayers)
                                              .FirstOrDefaultAsync(r => r.RoundId == round.RoundId);
            if (existingRound == null) return null;

            if (existingRound.GolfCourseId != round.GolfCourseId)
            {
                 if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == round.GolfCourseId))
                 {
                     throw new ArgumentException($"GolfCourse with ID {round.GolfCourseId} does not exist for update.");
                 }
            }

            _context.Entry(existingRound).CurrentValues.SetValues(round);

            if (playerIdsToUpdate != null)
            {
                // Remove players no longer in the list
                var playersToRemove = existingRound.RoundPlayers
                    .Where(rp => !playerIdsToUpdate.Contains(rp.PlayerId))
                    .ToList();
                _context.RoundPlayers.RemoveRange(playersToRemove);

                // Add new players
                var existingPlayerIds = existingRound.RoundPlayers.Select(rp => rp.PlayerId).ToList();
                foreach (var playerId in playerIdsToUpdate.Except(existingPlayerIds))
                {
                    if (!await _context.Players.AnyAsync(p => p.PlayerId == playerId)) continue; // Or throw
                    existingRound.RoundPlayers.Add(new RoundPlayer { RoundId = existingRound.RoundId, PlayerId = playerId });
                }
            }

            await _context.SaveChangesAsync();
            return existingRound;
        }
    }
}