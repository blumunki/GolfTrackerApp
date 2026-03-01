using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly IRoundService _roundService;
    private readonly IPlayerService _playerService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService, 
        IRoundService roundService,
        IPlayerService playerService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _roundService = roundService;
        _playerService = playerService;
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
    public async Task<ActionResult<List<object>>> GetRecentRounds([FromQuery] int limit = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var currentPlayer = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (currentPlayer == null)
            {
                _logger.LogWarning("Player not found for userId: {UserId}", userId);
                return Ok(new List<object>());
            }
            
            var playerId = currentPlayer.PlayerId;
            
            var domainRounds = await _roundService.GetRecentRoundsAsync(userId, false, limit);
            
            var apiRounds = (domainRounds ?? new List<Round>()).Select(r => new
            {
                RoundId = r.RoundId,
                DatePlayed = r.DatePlayed,
                CourseName = r.GolfCourse?.Name,
                ClubName = r.GolfCourse?.GolfClub?.Name,
                TotalScore = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Strokes),
                TotalPar = r.Scores.Where(s => s.PlayerId == playerId).Sum(s => s.Hole?.Par ?? 0)
            }).ToList();
            
            return Ok(apiRounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent rounds");
            return StatusCode(500, "An error occurred while retrieving recent rounds");
        }
    }

    [HttpGet("course-history")]
    public async Task<ActionResult<List<CourseHistoryItem>>> GetCourseHistory([FromQuery] int limit = 6)
    {
        try
        {
            var userId = GetCurrentUserId();
            var history = await _reportService.GetCourseHistoryAsync(userId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course history");
            return StatusCode(500, "An error occurred while retrieving course history");
        }
    }

    [HttpGet("scoring-distribution")]
    public async Task<ActionResult<ScoringDistribution>> GetScoringDistribution()
    {
        try
        {
            var userId = GetCurrentUserId();
            var currentPlayer = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (currentPlayer == null)
                return Ok(new ScoringDistribution());

            var distribution = await _reportService.GetScoringDistributionAsync(currentPlayer.PlayerId, null, null, null, null, null);
            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scoring distribution");
            return StatusCode(500, "An error occurred while retrieving scoring distribution");
        }
    }

    [HttpGet("performance-by-par")]
    public async Task<ActionResult<PerformanceByPar>> GetPerformanceByPar()
    {
        try
        {
            var userId = GetCurrentUserId();
            var currentPlayer = await _playerService.GetPlayerByApplicationUserIdAsync(userId);
            if (currentPlayer == null)
                return Ok(new PerformanceByPar());

            var performance = await _reportService.GetPerformanceByParAsync(currentPlayer.PlayerId, null, null, null, null, null);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance by par");
            return StatusCode(500, "An error occurred while retrieving performance by par");
        }
    }
}