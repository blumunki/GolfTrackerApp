using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class RoundService : IRoundService
    {

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<RoundService> _logger;

        public RoundService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<RoundService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Computes the par for a round based on holes played and course data.
        /// </summary>
        public static int ComputeRoundPar(Round r)
        {
            if (r.GolfCourse == null) return 72;

            if (r.HolesPlayed >= r.GolfCourse.NumberOfHoles)
                return r.GolfCourse.DefaultPar;

            if (r.GolfCourse.Holes?.Any() == true)
            {
                var actualPar = r.GolfCourse.Holes
                    .Where(h => h.HoleNumber >= r.StartingHole && h.HoleNumber < r.StartingHole + r.HolesPlayed)
                    .Sum(h => h.Par);
                if (actualPar > 0) return actualPar;
            }

            return (int)Math.Round((double)r.GolfCourse.DefaultPar * r.HolesPlayed / r.GolfCourse.NumberOfHoles);
        }

        public async Task<Round> AddRoundAsync(Round round, IEnumerable<int> playerIds)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            if (string.IsNullOrEmpty(round.CreatedByApplicationUserId))
            {
                throw new InvalidOperationException("Round must have a CreatedByApplicationUserId.");
            }
            if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == round.GolfCourseId))
            {
                throw new ArgumentException($"GolfCourse with ID {round.GolfCourseId} does not exist.");
            }
            if (round.StartingHole < 1 || round.HolesPlayed < 1)
            {
                throw new ArgumentException("Starting hole and holes played must be valid.");
            }

            if (playerIds == null || !playerIds.Any())
            {
                throw new ArgumentException("At least one player must be selected for the round.");
            }

            // Ensure all playerIds exist
            var existingPlayerIds = await _context.Players
                .Where(p => playerIds.Contains(p.PlayerId))
                .Select(p => p.PlayerId)
                .ToListAsync();
            
            var missingIds = playerIds.Except(existingPlayerIds).ToList();
            if (missingIds.Any())
            {
                throw new ArgumentException($"Player(s) with ID(s) {string.Join(", ", missingIds)} not found.");
            }

            // Use execution strategy to support SQL Server retrying execution strategy with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Rounds.Add(round);
                    await _context.SaveChangesAsync();

                    foreach (var playerId in playerIds)
                    {
                        round.RoundPlayers.Add(new RoundPlayer { RoundId = round.RoundId, PlayerId = playerId });
                    }
                    await _context.SaveChangesAsync();
                
                    await transaction.CommitAsync();
                    _logger.LogInformation("Created Round {RoundId} and linked {PlayerCount} players.", round.RoundId, playerIds.Count());
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return round;
        }


        public async Task<bool> DeleteRoundAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var round = await _context.Rounds.Include(r => r.RoundPlayers).Include(r => r.Scores).AsSplitQuery().FirstOrDefaultAsync(r => r.RoundId == id);
            if (round == null) return false;

            _context.Scores.RemoveRange(round.Scores); // Remove dependent scores
            _context.RoundPlayers.RemoveRange(round.RoundPlayers); // Remove join table entries
            _context.Rounds.Remove(round);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Round>> GetAllRoundsAsync(string requestingUserId, bool isUserAdmin)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            IQueryable<Round> query = _context.Rounds
                                        .Include(r => r.GolfCourse!)
                                            .ThenInclude(gc => gc!.GolfClub)
                                        .Include(r => r.RoundPlayers!)
                                            .ThenInclude(rp => rp!.Player!) // Ensure Player is included for filtering
                                                .ThenInclude(p => p!.ApplicationUser)
                                        .AsSplitQuery(); // For checking linked user

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
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Rounds
                                .AsNoTracking()
                                .Include(r => r.GolfCourse!)
                                    .ThenInclude(gc => gc!.GolfClub)
                                .Include(r => r.GolfCourse!)
                                    .ThenInclude(gc => gc!.Holes)
                                .Include(r => r.Scores!)
                                    .ThenInclude(s => s!.Hole)
                                .Include(r => r.RoundPlayers!)
                                    .ThenInclude(rp => rp!.Player)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(r => r.RoundId == id);
        }

        public async Task<List<Round>> GetRoundsForPlayerAsync(int playerId, string requestingUserId, bool isUserAdmin)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var playerToListRoundsFor = await _context.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (playerToListRoundsFor == null)
            {
                return new List<Round>(); // Player not found
            }

            IQueryable<Round> query = _context.Rounds
                .AsNoTracking()
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
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
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
        public async Task<Round> CreateRoundWithPlayersAsync(Round round, List<int> playerIds)
        {
            // Delegate to the consolidated method
            return await AddRoundAsync(round, playerIds);
        }
        public async Task<List<Round>> GetRecentRoundsAsync(string requestingUserId, bool isUserAdmin, int count)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // Find the PlayerId for the current ApplicationUser
            var currentPlayer = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == requestingUserId);

            // If the user doesn't have a player profile, they can't have played rounds.
            if (currentPlayer is null)
            {
                return new List<Round>();
            }

            IQueryable<Round> query = _context.Rounds
                                        .AsNoTracking()
                                        .Include(r => r.GolfCourse!)
                                            .ThenInclude(gc => gc!.GolfClub)
                                        .Include(r => r.Scores!)
                                            .ThenInclude(s => s.Hole)
                                        .AsSplitQuery();

            // The filter is now applied to EVERYONE, including admins.
            query = query.Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == currentPlayer.PlayerId));

            return await query.OrderByDescending(r => r.DatePlayed)
                            .Take(count)
                            .ToListAsync();
        }
        public async Task<List<Round>> SearchRoundsAsync(string requestingUserId, bool isUserAdmin, string searchTerm)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            IQueryable<Round> query = _context.Rounds
                                        .AsNoTracking()
                                        .Include(r => r.GolfCourse!)
                                            .ThenInclude(gc => gc!.GolfClub)
                                        .Include(r => r.RoundPlayers!)
                                            .ThenInclude(rp => rp!.Player!)
                                        .Include(r => r.Scores!)
                                            .ThenInclude(s => s.Hole!)
                                        .AsSplitQuery();

            if (!isUserAdmin)
            {
                query = query.Where(r =>
                    r.CreatedByApplicationUserId == requestingUserId ||
                    r.RoundPlayers.Any(rp => rp.Player != null && rp.Player.ApplicationUserId == requestingUserId)
                );
            }

            // Now, add the search filter using the EF.Functions.Like pattern
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                query = query.Where(r =>
                    (r.GolfCourse != null && EF.Functions.Like(r.GolfCourse.Name, pattern)) ||
                    (r.GolfCourse != null && r.GolfCourse.GolfClub != null && EF.Functions.Like(r.GolfCourse.GolfClub.Name, pattern)) ||
                    r.RoundPlayers.Any(rp => rp.Player != null && 
                        (EF.Functions.Like(rp.Player.FirstName, pattern) || EF.Functions.Like(rp.Player.LastName, pattern)))
                );
            }

            return await query.OrderByDescending(r => r.DatePlayed).ToListAsync();
        }

        public async Task<Scorecard> PrepareScorecardAsync(int courseId, int startingHole, int holesPlayed, List<Player> players)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var allCourseHoles = await _context.Holes
                .AsNoTracking()
                .Where(h => h.GolfCourseId == courseId)
                .OrderBy(h => h.HoleNumber)
                .ToListAsync();

            if (!allCourseHoles.Any())
            {
                throw new InvalidOperationException($"Course with ID {courseId} has no holes.");
            }

            // --- This is your logic, now centralized in the service ---
            var playedHoles = new List<Hole>();
            int currentHoleNum = startingHole;
            int maxHoleNumOnCourse = allCourseHoles.Max(h => h.HoleNumber);

            for (int i = 0; i < holesPlayed; i++)
            {
                var hole = allCourseHoles.FirstOrDefault(h => h.HoleNumber == currentHoleNum);
                if (hole != null)
                {
                    playedHoles.Add(hole);
                }
                currentHoleNum++;
                if (currentHoleNum > maxHoleNumOnCourse)
                {
                    currentHoleNum = 1;
                }
            }

            var scores = new Dictionary<int, List<HoleScoreEntryModel>>();
            foreach (var player in players)
            {
                var scoresForPlayer = playedHoles.Select(hole => new HoleScoreEntryModel
                {
                    HoleId = hole.HoleId,
                    HoleNumber = hole.HoleNumber,
                    Par = hole.Par,
                    StrokeIndex = hole.StrokeIndex ?? 0,
                    LengthYards = hole.LengthYards,
                }).ToList();
                scores.Add(player.PlayerId, scoresForPlayer);
            }
            // --- End of moved logic ---

            return new Scorecard
            {
                Players = players,
                PlayedHoles = playedHoles,
                Scores = scores
            };
        }

        public async Task<List<Round>> GetRecentRoundsForClubAsync(string requestingUserId, int clubId, int count)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // Get the player for this user
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == requestingUserId);

            if (player == null) return new List<Round>();

            return await _context.Rounds
                .AsNoTracking()
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId)
                           && r.Status == RoundCompletionStatus.Completed
                           && r.GolfCourse!.GolfClubId == clubId)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.Holes)
                .Include(r => r.Scores)
                .AsSplitQuery()
                .OrderByDescending(r => r.DatePlayed)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Round>> GetRecentRoundsForCourseAsync(string requestingUserId, int courseId, int count)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // Get the player for this user
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == requestingUserId);

            if (player == null) return new List<Round>();

            return await _context.Rounds
                .AsNoTracking()
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId)
                           && r.Status == RoundCompletionStatus.Completed
                           && r.GolfCourseId == courseId)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.Holes)
                .Include(r => r.Scores)
                .AsSplitQuery()
                .OrderByDescending(r => r.DatePlayed)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetRoundCountForClubAsync(string requestingUserId, int clubId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == requestingUserId);

            if (player == null) return 0;

            return await _context.Rounds
                .AsNoTracking()
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId)
                           && r.Status == RoundCompletionStatus.Completed
                           && r.GolfCourse!.GolfClubId == clubId)
                .CountAsync();
        }

        public async Task<int> GetRoundCountForCourseAsync(string requestingUserId, int courseId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == requestingUserId);

            if (player == null) return 0;

            return await _context.Rounds
                .AsNoTracking()
                .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == player.PlayerId)
                           && r.Status == RoundCompletionStatus.Completed
                           && r.GolfCourseId == courseId)
                .CountAsync();
        }
    }
}