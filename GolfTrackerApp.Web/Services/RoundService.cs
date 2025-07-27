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
        private readonly ILogger<PlayerService> _logger;

        public RoundService(ApplicationDbContext context, ILogger<PlayerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // In GolfTrackerApp.Web/Services/RoundService.cs
        public async Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds)
        {
            if (string.IsNullOrEmpty(round.CreatedByApplicationUserId))
            {
                throw new InvalidOperationException("Round must have a CreatedByApplicationUserId.");
            }
            // ... (rest of existing validation for GolfCourseId, players, etc.)
            if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == round.GolfCourseId)) // Example
            {
                throw new ArgumentException($"GolfCourse with ID {round.GolfCourseId} does not exist.");
            }
            if (round.StartingHole < 1 || round.HolesPlayed < 1)
            {
                throw new ArgumentException("Starting hole and holes played must be valid.");
            }
            // No need to check round.RoundType for null if it's a non-nullable enum and has [Required]

            if (playerIds == null || !playerIds.Any())
            {
                throw new ArgumentException("At least one player must be selected for the round.");
            }

            // Ensure all playerIds exist
            foreach (var playerId in playerIds)
            {
                if (!await _context.Players.AnyAsync(p => p.PlayerId == playerId))
                {
                    throw new ArgumentException($"Player with ID {playerId} not found.");
                }
            }

            _context.Rounds.Add(round);
            await _context.SaveChangesAsync(); // Save Round to get its ID

            foreach (var playerId in playerIds)
            {
                // RoundPlayers are just links, no scores are set here.
                round.RoundPlayers.Add(new RoundPlayer { RoundId = round.RoundId, PlayerId = playerId });
            }
            await _context.SaveChangesAsync(); // Save RoundPlayer links
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

        // In GolfTrackerApp.Web/Services/RoundService.cs
        public async Task<List<Round>> GetAllRoundsAsync(string requestingUserId, bool isUserAdmin)
        {
            IQueryable<Round> query = _context.Rounds
                                        .Include(r => r.GolfCourse!)
                                            .ThenInclude(gc => gc!.GolfClub)
                                        .Include(r => r.RoundPlayers!)
                                            .ThenInclude(rp => rp!.Player!) // Ensure Player is included for filtering
                                                .ThenInclude(p => p!.ApplicationUser); // For checking linked user

            if (!isUserAdmin)
            {
                // Regular users see rounds they created OR rounds they participated in
                // A player participated if their PlayerId is in RoundPlayers,
                // and if that Player record is linked to their ApplicationUser account.
                query = query.Where(r =>
                    r.CreatedByApplicationUserId == requestingUserId ||
                    r.RoundPlayers.Any(rp => rp.Player != null && rp.Player.ApplicationUserId == requestingUserId)
                );
            }

            return await query.OrderByDescending(r => r.DatePlayed).ThenBy(r => r.GolfCourse!.Name).ToListAsync();
        }

        public async Task<Round?> GetRoundByIdAsync(int id)
        {
            return await _context.Rounds
                                .Include(r => r.GolfCourse!)
                                    .ThenInclude(gc => gc!.GolfClub)
                                .Include(r => r.GolfCourse!)
                                    .ThenInclude(gc => gc!.Holes)
                                .Include(r => r.Scores!)
                                    .ThenInclude(s => s!.Hole)
                                .Include(r => r.RoundPlayers!)
                                    .ThenInclude(rp => rp!.Player)
                                .FirstOrDefaultAsync(r => r.RoundId == id);
        }

        // In GolfTrackerApp.Web/Services/RoundService.cs
        public async Task<List<Round>> GetRoundsForPlayerAsync(int playerId, string requestingUserId, bool isUserAdmin)
        {
            var playerToListRoundsFor = await _context.Players.FindAsync(playerId);
            if (playerToListRoundsFor == null)
            {
                return new List<Round>(); // Player not found
            }

            IQueryable<Round> query = _context.Rounds
                .Include(r => r.GolfCourse!.GolfClub)
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId));

            if (!isUserAdmin)
            {
                // Non-admin can see rounds for a player if:
                // 1. They are requesting their own rounds (playerToListRoundsFor is linked to requestingUserId)
                // 2. The playerToListRoundsFor is a managed player they created
                // 3. The round itself was created by them (requestingUserId)
                // This ensures they don't see rounds of other users' managed players unless they created the round.

                bool isOwnProfile = playerToListRoundsFor.ApplicationUserId == requestingUserId && !string.IsNullOrEmpty(requestingUserId);
                bool isOwnManagedPlayer = string.IsNullOrEmpty(playerToListRoundsFor.ApplicationUserId) && playerToListRoundsFor.CreatedByApplicationUserId == requestingUserId;

                if (!(isOwnProfile || isOwnManagedPlayer)) // If not viewing self or own managed player
                {
                    // Then, only show rounds that the requestingUser created that this player also played in.
                    // Or, if the round has broad visibility based on participation (already handled by initial query.Where)
                    // This can get complex. A simpler rule for now:
                    // If not admin, only show rounds created by the requestingUser OR rounds where requestingUser participated.
                    // The `query` already filters by playerId. We further filter if the requesting user is not an admin
                    // and is not directly related to the `playerId` as owner/self.
                    // The GetAllRoundsAsync logic is more about general list visibility.
                    // For GetRoundsForPlayerAsync, if they can query for a specific player,
                    // they should generally see those rounds if they have a reason to.
                    // For now, let's assume if they query by PlayerID, they see them,
                    // but the list of available players to query for would be filtered.
                    // So, the initial query by PlayerID might be sufficient, relying on UI to only allow querying valid players.
                    // However, to be stricter:
                    query = query.Where(r =>
                        r.CreatedByApplicationUserId == requestingUserId || // They created the round
                        r.RoundPlayers.Any(rp => rp.Player != null && rp.Player.ApplicationUserId == requestingUserId) // They participated
                    );
                }
            }
            return await query.OrderByDescending(r => r.DatePlayed).ToListAsync();
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
        // In RoundService.cs
        public async Task<Round> CreateRoundWithPlayersAsync(Round round, List<int> playerIds)
        {
            if (playerIds == null || !playerIds.Any())
            {
                throw new ArgumentException("A round must have at least one player.", nameof(playerIds));
            }

            // Add the new round to the context
            _context.Rounds.Add(round);

            // This will save the round and generate its RoundId
            await _context.SaveChangesAsync();

            // Now create the links in the RoundPlayers join table
            foreach (var playerId in playerIds)
            {
                var roundPlayer = new RoundPlayer { RoundId = round.RoundId, PlayerId = playerId };
                _context.RoundPlayers.Add(roundPlayer);
            }

            // Save the player links
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created Round {RoundId} and linked {PlayerCount} players.", round.RoundId, playerIds.Count);

            return round;
        }
    }
}