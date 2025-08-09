using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChartDataPoint>> GetPlayerPerformanceAsync(int playerId)
    {
        var performanceData = await _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed)
            .Include(r => r.Scores)
            .Include(r => r.GolfCourse)
            .OrderBy(r => r.DatePlayed)
            .Select(r => new ChartDataPoint
            {
                Date = r.DatePlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                CourseName = r.GolfCourse!.Name
            })
            .ToListAsync();

        return performanceData;
    }
}