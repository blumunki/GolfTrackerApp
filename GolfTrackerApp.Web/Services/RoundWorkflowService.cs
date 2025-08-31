using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services
{
    public class RoundWorkflowService : IRoundWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlayerService _playerService;
        private readonly IGolfClubService _golfClubService;
        private readonly IGolfCourseService _golfCourseService;
        private readonly IHoleService _holeService;
        private readonly IRoundService _roundService;
        private readonly ILogger<RoundWorkflowService> _logger;

        private readonly IScoreService _scoreService;

        public RoundWorkflowService(
            ApplicationDbContext context,
            IPlayerService playerService,
            IGolfClubService golfClubService,
            IGolfCourseService golfCourseService,
            IHoleService holeService,
            IRoundService roundService,
            IScoreService scoreService,
            ILogger<RoundWorkflowService> logger)
        {
            _context = context;
            _playerService = playerService;
            _golfClubService = golfClubService;
            _golfCourseService = golfCourseService;
            _holeService = holeService;
            _roundService = roundService;
            _scoreService = scoreService;
            _logger = logger;
        }

        public Task<RoundWorkflowSession> CreateNewRoundSessionAsync(string userId, bool isAdmin)
        {
            var session = new RoundWorkflowSession
            {
                UserId = userId,
                IsUserAdmin = isAdmin,
                DatePlayed = DateTime.Today
            };
            return Task.FromResult(session);
        }

        public async Task<RoundWorkflowSession> LoadExistingRoundSessionAsync(int roundId, string userId, bool isAdmin)
        {
            var round = await _roundService.GetRoundByIdAsync(roundId);
            if (round == null)
            {
                throw new ArgumentException($"Round with ID {roundId} not found.");
            }

            var session = new RoundWorkflowSession
            {
                ExistingRoundId = roundId,
                UserId = userId,
                IsUserAdmin = isAdmin,
                DatePlayed = round.DatePlayed,
                GolfCourseId = round.GolfCourseId,
                StartingHole = round.StartingHole,
                HolesPlayed = round.HolesPlayed,
                RoundType = round.RoundType,
                Notes = round.Notes,
                SelectedPlayers = round.RoundPlayers.Where(rp => rp.Player != null).Select(rp => rp.Player!).ToList()
            };

            // Load course info
            if (round.GolfCourse != null)
            {
                session.SelectedGolfClubId = round.GolfCourse.GolfClubId;
                session.CourseInfo = await GetCourseHoleInfoAsync(round.GolfCourseId);
            }

            // Load existing scores if any
            var scores = await _context.Scores
                .Include(s => s.Hole)
                .Where(s => s.RoundId == roundId)
                .ToListAsync();

            session.Scorecard = ConvertScoresToScorecard(scores, session.SelectedPlayers);

            return session;
        }

        public async Task<List<GolfClub>> GetAvailableGolfClubsAsync()
        {
            return await _golfClubService.GetAllGolfClubsAsync();
        }

        public async Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId)
        {
            var club = await _golfClubService.GetGolfClubByIdAsync(clubId);
            return club?.GolfCourses?.ToList() ?? new List<GolfCourse>();
        }

        public async Task<List<Player>> GetAvailablePlayersAsync(string userId, bool isAdmin)
        {
            return await _playerService.GetAllPlayersAsync(userId, isAdmin);
        }

        public async Task<List<Player>> SearchPlayersAsync(string searchTerm, string userId, bool isAdmin, List<int> excludePlayerIds)
        {
            var allPlayers = await GetAvailablePlayersAsync(userId, isAdmin);
            var availablePlayers = allPlayers.Where(p => !excludePlayerIds.Contains(p.PlayerId));

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return availablePlayers.OrderBy(p => p.FirstName).ThenBy(p => p.LastName).ToList();
            }

            return availablePlayers
                .Where(p => $"{p.FirstName} {p.LastName}".Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (p.ApplicationUser?.Email ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                .ToList();
        }

        public async Task<CourseHoleInfo> GetCourseHoleInfoAsync(int courseId)
        {
            var course = await _golfCourseService.GetGolfCourseByIdAsync(courseId);
            if (course == null)
            {
                throw new ArgumentException($"Course with ID {courseId} not found.");
            }

            var holes = await _holeService.GetHolesForCourseAsync(courseId);

            return new CourseHoleInfo
            {
                CourseId = courseId,
                CourseName = course.Name,
                ClubName = course.GolfClub?.Name ?? "",
                MaxHoles = holes.Any() ? holes.Max(h => h.HoleNumber) : 18,
                Holes = holes.OrderBy(h => h.HoleNumber).ToList()
            };
        }

        public async Task<RoundWorkflowSession> UpdateRoundSetupAsync(RoundWorkflowSession session, RoundSetupData setupData)
        {
            session.DatePlayed = setupData.DatePlayed;
            session.GolfCourseId = setupData.GolfCourseId;
            session.StartingHole = setupData.StartingHole;
            session.HolesPlayed = setupData.HolesPlayed;
            session.RoundType = setupData.RoundType;
            session.Notes = setupData.Notes;

            // Update course info if course changed
            if (setupData.GolfCourseId > 0)
            {
                session.CourseInfo = await GetCourseHoleInfoAsync(setupData.GolfCourseId);
                
                // Adjust holes played if it exceeds course maximum
                if (session.CourseInfo.MaxHoles < session.HolesPlayed)
                {
                    session.HolesPlayed = session.CourseInfo.MaxHoles;
                }
            }

            await ValidateRoundSetupAsync(session);
            return session;
        }

        public async Task<RoundWorkflowSession> AddPlayerToRoundAsync(RoundWorkflowSession session, int playerId)
        {
            if (session.SelectedPlayers.Any(p => p.PlayerId == playerId))
            {
                return session; // Player already added
            }

            var players = await GetAvailablePlayersAsync(session.UserId, session.IsUserAdmin);
            var player = players.FirstOrDefault(p => p.PlayerId == playerId);
            
            if (player != null)
            {
                session.SelectedPlayers.Add(player);
                _logger.LogInformation("Player '{FirstName} {LastName}' added to round.", player.FirstName, player.LastName);
            }

            return session;
        }

        public Task<RoundWorkflowSession> RemovePlayerFromRoundAsync(RoundWorkflowSession session, int playerId)
        {
            var player = session.SelectedPlayers.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                session.SelectedPlayers.Remove(player);
                
                // Remove scores for this player if they exist
                if (session.Scorecard.ContainsKey(playerId))
                {
                    session.Scorecard.Remove(playerId);
                }
                
                _logger.LogInformation("Player '{FirstName} {LastName}' removed from round.", player.FirstName, player.LastName);
            }

            return Task.FromResult(session);
        }

        public async Task<Scorecard> PrepareScorecardAsync(RoundWorkflowSession session)
        {
            if (session.CourseInfo == null || !session.SelectedPlayers.Any())
            {
                throw new InvalidOperationException("Course info and players are required to prepare scorecard.");
            }

            return await _roundService.PrepareScorecardAsync(
                session.GolfCourseId,
                session.StartingHole,
                session.HolesPlayed,
                session.SelectedPlayers);
        }

        public Task<RoundWorkflowSession> UpdateScoresAsync(RoundWorkflowSession session, Dictionary<int, List<HoleScoreEntryModel>> scores)
        {
            session.Scorecard = scores;
            return Task.FromResult(session);
        }

        public async Task<int> SaveRoundAsync(RoundWorkflowSession session)
        {
            if (!await ValidateRoundSetupAsync(session))
            {
                throw new InvalidOperationException($"Round validation failed: {string.Join(", ", session.ValidationErrors)}");
            }

            if (session.IsEditMode)
            {
                return await UpdateExistingRoundAsync(session);
            }
            else
            {
                return await CreateNewRoundAsync(session);
            }
        }

        public Task<bool> ValidateRoundSetupAsync(RoundWorkflowSession session)
        {
            session.ValidationErrors.Clear();

            if (!session.DatePlayed.HasValue)
            {
                session.ValidationErrors.Add("Date played is required.");
            }

            if (session.GolfCourseId <= 0)
            {
                session.ValidationErrors.Add("Golf course must be selected.");
            }

            if (session.StartingHole < 1)
            {
                session.ValidationErrors.Add("Starting hole must be valid.");
            }

            if (session.HolesPlayed < 1)
            {
                session.ValidationErrors.Add("Holes played must be valid.");
            }

            if (!session.SelectedPlayers.Any())
            {
                session.ValidationErrors.Add("At least one player must be selected.");
            }

            if (session.CourseInfo != null && session.HolesPlayed > session.CourseInfo.MaxHoles)
            {
                session.ValidationErrors.Add($"Holes played cannot exceed course maximum of {session.CourseInfo.MaxHoles}.");
            }

            return Task.FromResult(session.IsValid);
        }

        public async Task<Player> CreateGuestPlayerAsync(string firstName, string lastName, string userId)
        {
            var guestPlayer = new Player
            {
                FirstName = firstName,
                LastName = lastName,
                CreatedByApplicationUserId = userId
            };

            return await _playerService.AddPlayerAsync(guestPlayer);
        }

        private async Task<int> CreateNewRoundAsync(RoundWorkflowSession session)
        {
            var round = new Round
            {
                DatePlayed = session.DatePlayed!.Value,
                GolfCourseId = session.GolfCourseId,
                StartingHole = session.StartingHole,
                HolesPlayed = session.HolesPlayed,
                RoundType = session.RoundType,
                Notes = session.Notes,
                CreatedByApplicationUserId = session.UserId,
                Status = session.Scorecard.Any() ? RoundCompletionStatus.Completed : RoundCompletionStatus.InProgress
            };

            var playerIds = session.SelectedPlayers.Select(p => p.PlayerId);
            var savedRound = await _roundService.AddRoundAsync(round, playerIds);

            // Save scores if any exist
            if (session.Scorecard.Any())
            {
                await _scoreService.SaveScorecardAsync(savedRound.RoundId, session.Scorecard);
            }

            return savedRound.RoundId;
        }

        private async Task<int> UpdateExistingRoundAsync(RoundWorkflowSession session)
        {
            var round = await _roundService.GetRoundByIdAsync(session.ExistingRoundId!.Value);
            if (round == null)
            {
                throw new ArgumentException("Round not found for update.");
            }

            // Update round properties
            round.DatePlayed = session.DatePlayed!.Value;
            round.Notes = session.Notes;
            // Note: We typically don't allow changing course/holes for existing rounds

            var playerIds = session.SelectedPlayers.Select(p => p.PlayerId);
            await _roundService.UpdateRoundAsync(round, playerIds);

            // Update scores
            if (session.Scorecard.Any())
            {
                await _scoreService.SaveScorecardAsync(session.ExistingRoundId.Value, session.Scorecard);
            }

            return session.ExistingRoundId.Value;
        }

        private Dictionary<int, List<HoleScoreEntryModel>> ConvertScoresToScorecard(List<Score> scores, List<Player> players)
        {
            var scorecard = new Dictionary<int, List<HoleScoreEntryModel>>();

            foreach (var player in players)
            {
                var playerScores = scores.Where(s => s.PlayerId == player.PlayerId).ToList();
                var holeScores = new List<HoleScoreEntryModel>();

                foreach (var score in playerScores)
                {
                    if (score.Hole != null)
                    {
                        holeScores.Add(new HoleScoreEntryModel
                        {
                            HoleId = score.HoleId,
                            HoleNumber = score.Hole.HoleNumber,
                            Par = score.Hole.Par,
                            StrokeIndex = score.Hole.StrokeIndex ?? 0,
                            LengthYards = score.Hole.LengthYards,
                            Strokes = score.Strokes,
                            Putts = score.Putts,
                            FairwayHit = score.FairwayHit
                        });
                    }
                }

                scorecard[player.PlayerId] = holeScores.OrderBy(h => h.HoleNumber).ToList();
            }

            return scorecard;
        }
    }
}
