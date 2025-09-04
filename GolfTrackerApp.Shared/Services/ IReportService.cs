using GolfTrackerApp.Shared.Models;

namespace GolfTrackerApp.Shared.Services;

public interface IReportService
{
    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<List<PlayingPartnerSummary>> GetPlayingPartnerSummaryAsync(string currentUserId, int count);

    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceSummaryAsync(string currentUserId, int roundCount);

    Task<PlayerReportViewModel> GetPlayerReportViewModelAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<ScoringDistribution> GetScoringDistributionAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<PerformanceByPar> GetPerformanceByParAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    // New methods for club/course specific data
    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceForClubAsync(string currentUserId, int clubId, int roundCount);
    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceForCourseAsync(string currentUserId, int courseId, int roundCount);
    
    // Dashboard statistics
    Task<DashboardStats> GetDashboardStatsAsync(string currentUserId);
}