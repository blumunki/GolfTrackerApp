using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Models.Api;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/players/merge")]
public class MergeController : BaseApiController
{
    private readonly IMergeService _mergeService;
    private readonly ILogger<MergeController> _logger;

    public MergeController(IMergeService mergeService, ILogger<MergeController> logger)
    {
        _mergeService = mergeService;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<ActionResult> RequestMerge([FromBody] MergeRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var merge = await _mergeService.RequestMergeAsync(
                userId, request.SourcePlayerId, request.TargetUserId, request.Message);
            return Ok(new { merge.Id, Status = merge.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting merge");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("pending-received")]
    public async Task<ActionResult> GetPendingMergeRequestsReceived()
    {
        try
        {
            var userId = GetCurrentUserId();
            var requests = await _mergeService.GetPendingMergeRequestsReceivedAsync(userId);
            
            var result = requests.Select(m => new MergeResponseDto
            {
                Id = m.Id,
                RequestingUserName = m.RequestingUser?.UserName ?? "Unknown",
                SourcePlayerName = m.SourcePlayer != null ? $"{m.SourcePlayer.FirstName} {m.SourcePlayer.LastName}" : "Unknown",
                TargetPlayerName = m.TargetPlayer != null ? $"{m.TargetPlayer.FirstName} {m.TargetPlayer.LastName}" : "Unknown",
                Message = m.Message,
                RequestedAt = m.RequestedAt,
                Status = m.Status.ToString()
            }).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending merge requests");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("pending-sent")]
    public async Task<ActionResult> GetPendingMergeRequestsSent()
    {
        try
        {
            var userId = GetCurrentUserId();
            var requests = await _mergeService.GetPendingMergeRequestsSentAsync(userId);
            
            var result = requests.Select(m => new MergeResponseDto
            {
                Id = m.Id,
                TargetUserName = m.TargetUser?.UserName ?? "Unknown",
                SourcePlayerName = m.SourcePlayer != null ? $"{m.SourcePlayer.FirstName} {m.SourcePlayer.LastName}" : "Unknown",
                TargetPlayerName = m.TargetPlayer != null ? $"{m.TargetPlayer.FirstName} {m.TargetPlayer.LastName}" : "Unknown",
                Message = m.Message,
                RequestedAt = m.RequestedAt,
                Status = m.Status.ToString()
            }).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sent merge requests");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{mergeId}/accept")]
    public async Task<ActionResult> AcceptMerge(int mergeId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _mergeService.AcceptMergeRequestAsync(mergeId, userId);
            if (result == null)
                return NotFound("Merge request not found");
            return Ok(new { result.Id, Status = result.Status.ToString(), result.RoundsMerged, result.RoundsSkipped });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting merge {Id}", mergeId);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{mergeId}/decline")]
    public async Task<ActionResult> DeclineMerge(int mergeId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _mergeService.DeclineMergeRequestAsync(mergeId, userId);
            if (result == null)
                return NotFound("Merge request not found");
            return Ok(new { result.Id, Status = result.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining merge {Id}", mergeId);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("mergeable/{targetUserId}")]
    public async Task<ActionResult<List<Player>>> GetMergeablePlayersAsync(string targetUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var players = await _mergeService.GetMergeablePlayersAsync(userId, targetUserId);
            return Ok(players);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mergeable players");
            return StatusCode(500, "An error occurred");
        }
    }
}
