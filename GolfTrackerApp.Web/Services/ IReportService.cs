using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IReportService
{
    Task<List<ChartDataPoint>> GetPlayerPerformanceAsync(int playerId);
}