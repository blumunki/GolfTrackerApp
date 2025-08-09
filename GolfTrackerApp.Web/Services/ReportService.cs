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

    public async Task<List<PlayerPerformanceDataPoint>> GetPlayerPerformanceAsync(int playerId, int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Rounds
            .Where(r => r.RoundPlayers.Any(rp => rp.PlayerId == playerId) && r.Status == RoundCompletionStatus.Completed);

        // Apply filters
        if (courseId.HasValue && courseId > 0)
        {
            query = query.Where(r => r.GolfCourseId == courseId.Value);
        }
        if (holesPlayed.HasValue && holesPlayed > 0)
        {
            query = query.Where(r => r.HolesPlayed == holesPlayed.Value);
        }
        if (roundType.HasValue)
        {
            query = query.Where(r => r.RoundType == roundType.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Date >= startDate.Value.Date);
        }
        if (endDate.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Date <= endDate.Value.Date);
        }

        var performanceData = await query
            .Include(r => r.Scores)
            .ThenInclude(s => s.Hole)
            .Include(r => r.GolfCourse)
            .ThenInclude(gc => gc!.GolfClub) // Eager load GolfClub
            .OrderBy(r => r.DatePlayed)
            .Select(r => new PlayerPerformanceDataPoint
            {
                Date = r.DatePlayed,
                CourseName = $"{r.GolfCourse!.GolfClub!.Name} - {r.GolfCourse.Name}",
                HolesPlayed = r.HolesPlayed,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole!.Par)
            })
            .ToListAsync();

        return performanceData;
    }
}