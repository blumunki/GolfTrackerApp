using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IReportService
{
    Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate);
}