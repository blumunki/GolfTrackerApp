using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class InsightsController : BaseApiController
{
    private readonly IAiInsightService _insightService;
    private readonly IAiChatService _chatService;
    private readonly ILogger<InsightsController> _logger;

    public InsightsController(
        IAiInsightService insightService,
        IAiChatService chatService,
        ILogger<InsightsController> logger)
    {
        _insightService = insightService;
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AiInsightResult>> GetDashboardInsights(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetDashboardInsightsAsync(userId, cancellationToken: cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard insights");
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("player-report/{playerId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetPlayerReportInsights(
        int playerId,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _insightService.GetPlayerReportInsightsAsync(
                playerId, courseId, holesPlayed, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player report insights for {PlayerId}", playerId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("club/{clubId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetClubInsights(
        int clubId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetClubInsightsAsync(userId, clubId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting club insights for {ClubId}", clubId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpGet("course/{courseId:int}")]
    public async Task<ActionResult<AiInsightResult>> GetCourseInsights(
        int courseId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.GetCourseInsightsAsync(userId, courseId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course insights for {CourseId}", courseId);
            return StatusCode(500, "Failed to generate insights.");
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<AiInsightResult>> Chat(
        [FromBody] AiChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _insightService.ChatAsync(
                userId, request.Message, request.SessionId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI chat");
            return StatusCode(500, "Failed to process chat message.");
        }
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<List<AiChatSession>>> GetChatSessions(
        [FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var sessions = await _chatService.GetSessionsAsync(userId, limit);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat sessions");
            return StatusCode(500, "Failed to load chat sessions.");
        }
    }

    [HttpGet("sessions/{sessionId:int}")]
    public async Task<ActionResult<AiChatSession>> GetChatSession(int sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionAsync(sessionId, userId);
            if (session == null) return NotFound();
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat session {SessionId}", sessionId);
            return StatusCode(500, "Failed to load chat session.");
        }
    }
}
