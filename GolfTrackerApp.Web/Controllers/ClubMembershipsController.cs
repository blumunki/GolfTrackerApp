using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class ClubMembershipsController : BaseApiController
{
    private readonly IClubMembershipService _membershipService;
    private readonly ILogger<ClubMembershipsController> _logger;

    public ClubMembershipsController(IClubMembershipService membershipService, ILogger<ClubMembershipsController> logger)
    {
        _membershipService = membershipService;
        _logger = logger;
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<object>>> GetMyMemberships()
    {
        try
        {
            var userId = GetCurrentUserId();
            var memberships = await _membershipService.GetMembershipsForUserAsync(userId);
            var result = memberships.Select(cm => new
            {
                cm.ClubMembershipId,
                cm.GolfClubId,
                ClubName = cm.GolfClub?.Name,
                Role = cm.Role.ToString(),
                cm.MembershipNumber,
                cm.JoinedAt
            }).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving club memberships");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{clubId}/join")]
    public async Task<ActionResult> JoinClub(int clubId, [FromBody] JoinClubRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var membership = await _membershipService.JoinClubAsync(clubId, userId, request?.MembershipNumber);
            return Ok(new { membership.ClubMembershipId, Role = membership.Role.ToString(), membership.JoinedAt });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining club {ClubId}", clubId);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{clubId}/leave")]
    public async Task<ActionResult> LeaveClub(int clubId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _membershipService.LeaveClubAsync(clubId, userId);
            if (!result) return NotFound();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving club {ClubId}", clubId);
            return StatusCode(500, "An error occurred");
        }
    }
}

public class JoinClubRequest
{
    public string? MembershipNumber { get; set; }
}
