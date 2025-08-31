using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IReportService
{
    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<List<PlayingPartnerSummary>> GetPlayingPartnerSummaryAsync(string currentUserId, int count);

    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceSummaryAsync(string currentUserId, int roundCount);

    Task<PlayerReportViewModel> GetPlayerReportViewModelAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<ScoringDistribution> GetScoringDistributionAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);

    Task<PerformanceByPar> GetPerformanceByParAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);
}