using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class DashboardController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IReportService reportService, ILogger<DashboardController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetDashboardStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            var stats = await _reportService.GetDashboardStatsAsync(userId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return StatusCode(500, "An error occurred while retrieving dashboard stats");
        }
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<List<string>>> GetRecentActivity([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            var activity = await _reportService.GetRecentActivityAsync(userId, limit);
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return StatusCode(500, "An error occurred while retrieving recent activity");
        }
    }

    [HttpGet("score-distribution")]
    public async Task<ActionResult<List<ScoreDistributionBucket>>> GetScoreDistribution()
    {
        try
        {
            var userId = GetCurrentUserId();
            var distribution = await _reportService.GetScoreDistributionBucketedAsync(userId);
            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving score distribution");
            return StatusCode(500, "An error occurred while retrieving score distribution");
        }
    }

    [HttpGet("favorite-courses")]
    public async Task<ActionResult<List<FavoriteCourseItem>>> GetFavoriteCourses([FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var courses = await _reportService.GetFavoriteCoursesAsync(userId, limit);
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorite courses");
            return StatusCode(500, "An error occurred while retrieving favorite courses");
        }
    }
}
