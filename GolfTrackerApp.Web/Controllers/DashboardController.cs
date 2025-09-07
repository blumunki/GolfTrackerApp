using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class DashboardController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDashboardStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var totalRounds = await _context.Rounds
                .Where(r => r.CreatedByApplicationUserId == userId)
                .CountAsync();
            
            var roundScores = await _context.Scores
                .Where(s => s.Round != null && s.Round.CreatedByApplicationUserId == userId)
                .GroupBy(s => s.RoundId)
                .Select(g => g.Sum(s => s.Strokes))
                .ToListAsync();

            var averageScore = roundScores.Any() ? roundScores.Average() : 0.0;
            var bestScore = roundScores.Any() ? roundScores.Min() : 0;

            var coursesPlayed = await _context.Rounds
                .Where(r => r.CreatedByApplicationUserId == userId)
                .Select(r => r.GolfCourseId)
                .Distinct()
                .CountAsync();

            var stats = new
            {
                TotalRounds = totalRounds,
                AverageScore = totalRounds > 0 ? Math.Round(averageScore, 1) : (double?)null,
                BestScore = bestScore,
                CoursesPlayed = coursesPlayed
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return StatusCode(500, "An error occurred while retrieving dashboard stats");
        }
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<List<string>>> GetRecentActivity()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var recentRounds = await _context.Rounds
                .Include(r => r.GolfCourse)
                .ThenInclude(gc => gc!.GolfClub!)
                .Where(r => r.CreatedByApplicationUserId == userId)
                .OrderByDescending(r => r.DatePlayed)
                .Take(5)
                .Select(r => $"Played {(r.GolfCourse != null ? r.GolfCourse.Name : "Unknown Course")} on {r.DatePlayed:MMM dd}")
                .ToListAsync();

            if (!recentRounds.Any())
            {
                recentRounds = new List<string>
                {
                    "Welcome to Golf Tracker!",
                    "Start by recording your first round",
                    "Explore golf clubs in your area"
                };
            }

            return Ok(recentRounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return StatusCode(500, "An error occurred while retrieving recent activity");
        }
    }

    [HttpGet("score-distribution")]
    public async Task<ActionResult<List<object>>> GetScoreDistribution()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var roundScores = await _context.Scores
                .Where(s => s.Round != null && s.Round.CreatedByApplicationUserId == userId)
                .GroupBy(s => s.RoundId)
                .Select(g => g.Sum(s => s.Strokes))
                .ToListAsync();

            if (!roundScores.Any())
            {
                return Ok(new List<object>());
            }

            var totalRounds = roundScores.Count;
            
            var distribution = new List<object>
            {
                new { Range = "Under 80", Count = roundScores.Count(s => s < 80), Percentage = Math.Round((double)roundScores.Count(s => s < 80) / totalRounds * 100, 1) },
                new { Range = "80-89", Count = roundScores.Count(s => s >= 80 && s <= 89), Percentage = Math.Round((double)roundScores.Count(s => s >= 80 && s <= 89) / totalRounds * 100, 1) },
                new { Range = "90-99", Count = roundScores.Count(s => s >= 90 && s <= 99), Percentage = Math.Round((double)roundScores.Count(s => s >= 90 && s <= 99) / totalRounds * 100, 1) },
                new { Range = "100+", Count = roundScores.Count(s => s >= 100), Percentage = Math.Round((double)roundScores.Count(s => s >= 100) / totalRounds * 100, 1) }
            };

            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving score distribution");
            return StatusCode(500, "An error occurred while retrieving score distribution");
        }
    }

    [HttpGet("favorite-courses")]
    public async Task<ActionResult<List<object>>> GetFavoriteCourses()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var favoriteCourses = await _context.Rounds
                .Include(r => r.GolfCourse)
                .ThenInclude(gc => gc!.GolfClub!)
                .Where(r => r.CreatedByApplicationUserId == userId)
                .GroupBy(r => new { r.GolfCourseId, CourseName = r.GolfCourse!.Name, ClubName = r.GolfCourse.GolfClub!.Name })
                .Select(g => new
                {
                    Name = g.Key.CourseName,
                    Location = g.Key.ClubName,
                    RoundsPlayed = g.Count(),
                    AverageScore = Math.Round(g.SelectMany(r => r.Scores).GroupBy(s => s.RoundId).Average(sg => sg.Sum(s => s.Strokes)), 1)
                })
                .OrderByDescending(fc => fc.RoundsPlayed)
                .Take(10)
                .ToListAsync();

            return Ok(favoriteCourses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorite courses");
            return StatusCode(500, "An error occurred while retrieving favorite courses");
        }
    }
}
