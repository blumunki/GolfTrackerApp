using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Models.Api;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/players/connections")]
public class ConnectionsController : BaseApiController
{
    private readonly IConnectionService _connectionService;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(IConnectionService connectionService, ILogger<ConnectionsController> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetConnections()
    {
        try
        {
            var userId = GetCurrentUserId();
            var connections = await _connectionService.GetConnectionsAsync(userId);
            
            var result = connections.Select(c => new ConnectionDto
            {
                Id = c.Id,
                ConnectedUserId = c.RequestingUserId == userId ? c.TargetUserId : c.RequestingUserId,
                ConnectedUserName = c.RequestingUserId == userId 
                    ? c.TargetUser?.UserName ?? "Unknown"
                    : c.RequestingUser?.UserName ?? "Unknown",
                Status = c.Status.ToString(),
                ConnectedSince = c.RespondedAt
            }).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connections");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("pending-received")]
    public async Task<ActionResult> GetPendingRequestsReceived()
    {
        try
        {
            var userId = GetCurrentUserId();
            var requests = await _connectionService.GetPendingRequestsReceivedAsync(userId);
            
            var result = requests.Select(c => new ConnectionDto
            {
                Id = c.Id,
                ConnectedUserId = c.RequestingUserId,
                ConnectedUserName = c.RequestingUser?.UserName ?? "Unknown",
                Status = c.Status.ToString(),
                RequestedAt = c.RequestedAt
            }).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests received");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("pending-sent")]
    public async Task<ActionResult> GetPendingRequestsSent()
    {
        try
        {
            var userId = GetCurrentUserId();
            var requests = await _connectionService.GetPendingRequestsSentAsync(userId);
            
            var result = requests.Select(c => new ConnectionDto
            {
                Id = c.Id,
                ConnectedUserId = c.TargetUserId,
                ConnectedUserName = c.TargetUser?.UserName ?? "Unknown",
                Status = c.Status.ToString(),
                RequestedAt = c.RequestedAt
            }).ToList();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests sent");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserSearchResult>>> SearchUsers([FromQuery] string searchTerm)
    {
        try
        {
            var userId = GetCurrentUserId();
            var results = await _connectionService.SearchUsersAsync(searchTerm, userId);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("request")]
    public async Task<ActionResult> SendConnectionRequest([FromBody] ConnectionRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var connection = await _connectionService.SendConnectionRequestAsync(userId, request.TargetUserId);
            return Ok(new { connection.Id, Status = connection.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending connection request");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{connectionId}/accept")]
    public async Task<ActionResult> AcceptConnection(int connectionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _connectionService.AcceptConnectionRequestAsync(connectionId, userId);
            if (result == null)
                return NotFound("Connection request not found");
            return Ok(new { result.Id, Status = result.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting connection {Id}", connectionId);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{connectionId}/decline")]
    public async Task<ActionResult> DeclineConnection(int connectionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _connectionService.DeclineConnectionRequestAsync(connectionId, userId);
            if (result == null)
                return NotFound("Connection request not found");
            return Ok(new { result.Id, Status = result.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining connection {Id}", connectionId);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{connectionId}")]
    public async Task<ActionResult> RemoveConnection(int connectionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var removed = await _connectionService.RemoveConnectionAsync(connectionId, userId);
            if (!removed)
                return NotFound("Connection not found");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {Id}", connectionId);
            return StatusCode(500, "An error occurred");
        }
    }
}
