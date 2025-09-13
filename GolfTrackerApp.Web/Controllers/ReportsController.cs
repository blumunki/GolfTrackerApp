using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly IRoundService _roundService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService, 
        IRoundService roundService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _roundService = roundService;
        _logger = logger;
    }

    [HttpGet("dashboard-stats")]
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

    [HttpGet("playing-partners")]
    public async Task<ActionResult<List<PlayingPartnerSummary>>> GetPlayingPartners([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            var partners = await _reportService.GetPlayingPartnerSummaryAsync(userId, limit);
            return Ok(partners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving playing partners");
            return StatusCode(500, "An error occurred while retrieving playing partners");
        }
    }

    [HttpGet("performance-summary")]
    public async Task<ActionResult<List<PlayerPerformanceDataPoint>>> GetPerformanceSummary([FromQuery] int roundCount = 7)
    {
        try
        {
            var userId = GetCurrentUserId();
            var performance = await _reportService.GetPlayerPerformanceSummaryAsync(userId, roundCount);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance summary");
            return StatusCode(500, "An error occurred while retrieving performance summary");
        }
    }

    [HttpGet("recent-rounds")]
    public async Task<ActionResult<List<Round>>> GetRecentRounds([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            var rounds = await _roundService.GetRecentRoundsAsync(userId, false, limit);
            return Ok(rounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent rounds");
            return StatusCode(500, "An error occurred while retrieving recent rounds");
        }
    }
}