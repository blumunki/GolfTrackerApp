using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IRoundWorkflowService
    {
        Task<RoundWorkflowSession> CreateNewRoundSessionAsync(string userId, bool isAdmin);
        Task<RoundWorkflowSession> LoadExistingRoundSessionAsync(int roundId, string userId, bool isAdmin);
        Task<List<GolfClub>> GetAvailableGolfClubsAsync();
        Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId);
        Task<List<Player>> GetAvailablePlayersAsync(string userId, bool isAdmin);
        Task<List<Player>> SearchPlayersAsync(string searchTerm, string userId, bool isAdmin, List<int> excludePlayerIds);
        Task<CourseHoleInfo> GetCourseHoleInfoAsync(int courseId);
        Task<RoundWorkflowSession> UpdateRoundSetupAsync(RoundWorkflowSession session, RoundSetupData setupData);
        Task<RoundWorkflowSession> AddPlayerToRoundAsync(RoundWorkflowSession session, int playerId);
        Task<RoundWorkflowSession> RemovePlayerFromRoundAsync(RoundWorkflowSession session, int playerId);
        Task<Scorecard> PrepareScorecardAsync(RoundWorkflowSession session);
        Task<RoundWorkflowSession> UpdateScoresAsync(RoundWorkflowSession session, Dictionary<int, List<HoleScoreEntryModel>> scores);
        Task<int> SaveRoundAsync(RoundWorkflowSession session);
        Task<bool> ValidateRoundSetupAsync(RoundWorkflowSession session);
        Task<Player> CreateGuestPlayerAsync(string firstName, string lastName, string userId);
    }

    public class RoundWorkflowSession
    {
        public int? ExistingRoundId { get; set; }
        public bool IsEditMode => ExistingRoundId.HasValue;
        public string UserId { get; set; } = string.Empty;
        public bool IsUserAdmin { get; set; }
        
        // Round Setup Data
        public DateTime? DatePlayed { get; set; } = DateTime.Today;
        public int SelectedGolfClubId { get; set; }
        public int GolfCourseId { get; set; }
        public int StartingHole { get; set; } = 1;
        public int HolesPlayed { get; set; } = 18;
        public RoundTypeOption RoundType { get; set; } = RoundTypeOption.Friendly;
        public string? Notes { get; set; }
        
        // Players
        public List<Player> SelectedPlayers { get; set; } = new();
        
        // Course Info
        public CourseHoleInfo? CourseInfo { get; set; }
        
        // Scores
        public Dictionary<int, List<HoleScoreEntryModel>> Scorecard { get; set; } = new();
        
        // Validation State
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => !ValidationErrors.Any();
    }

    public class RoundSetupData
    {
        public DateTime? DatePlayed { get; set; }
        public int GolfCourseId { get; set; }
        public int StartingHole { get; set; }
        public int HolesPlayed { get; set; }
        public RoundTypeOption RoundType { get; set; }
        public string? Notes { get; set; }
    }

    public class CourseHoleInfo
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public int MaxHoles { get; set; }
        public List<Hole> Holes { get; set; } = new();
    }
}
