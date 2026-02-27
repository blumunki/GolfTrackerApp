using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class PlayersController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlayersController> _logger;
    private readonly IReportService _reportService;
    private readonly IConnectionService _connectionService;
    private readonly IMergeService _mergeService;

    public PlayersController(
        ApplicationDbContext context, 
        ILogger<PlayersController> logger, 
        IReportService reportService,
        IConnectionService connectionService,
        IMergeService mergeService)
    {
        _context = context;
        _logger = logger;
        _reportService = reportService;
        _connectionService = connectionService;
        _mergeService = mergeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Player>>> GetAllPlayers()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            var players = await _context.Players
                .Where(p => p.CreatedByApplicationUserId == userId)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            
            return Ok(players);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving players");
            return StatusCode(500, "An error occurred while retrieving players");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> GetPlayer(int id)
    {
        try
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound($"Player with ID {id} not found");
            }
            return Ok(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the player");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Player>>> SearchPlayers([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var players = await _context.Players
                .Where(p => p.FirstName.Contains(searchTerm) || 
                           p.LastName.Contains(searchTerm))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            
            return Ok(players);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching players with term: {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching players");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Player>> CreatePlayer([FromBody] Player player)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetPlayer), new { id = player.PlayerId }, player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating player");
            return StatusCode(500, "An error occurred while creating the player");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<Player>> UpdatePlayer(int id, [FromBody] Player player)
    {
        try
        {
            if (id != player.PlayerId)
            {
                return BadRequest("Player ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(player).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return Ok(player);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Players.AnyAsync(p => p.PlayerId == id))
            {
                return NotFound($"Player with ID {id} not found");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player {PlayerId}", id);
            return StatusCode(500, "An error occurred while updating the player");
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeletePlayer(int id)
    {
        try
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound($"Player with ID {id} not found");
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting player {PlayerId}", id);
            return StatusCode(500, "An error occurred while deleting the player");
        }
    }

    [HttpGet("{id}/report")]
    public async Task<ActionResult<PlayerReportViewModel>> GetPlayerReport(
        int id,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        [FromQuery] RoundTypeOption? roundType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? compareWith = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // First verify the player belongs to the current user
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == id && p.CreatedByApplicationUserId == userId);
                
            if (player == null)
            {
                return NotFound($"Player with ID {id} not found or not accessible");
            }

            var sharedWithPlayerIds = ParseCompareWith(compareWith);
            var report = await _reportService.GetPlayerReportViewModelAsync(id, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player report for player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the player report");
        }
    }

    [HttpGet("{id}/scoring-distribution")]
    public async Task<ActionResult<ScoringDistribution>> GetScoringDistribution(
        int id,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        [FromQuery] RoundTypeOption? roundType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? compareWith = null)
    {
        try
        {
            var sharedWithPlayerIds = ParseCompareWith(compareWith);
            var distribution = await _reportService.GetScoringDistributionAsync(id, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);
            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scoring distribution for player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the scoring distribution");
        }
    }

    [HttpGet("{id}/performance-by-par")]
    public async Task<ActionResult<PerformanceByPar>> GetPerformanceByPar(
        int id,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        [FromQuery] RoundTypeOption? roundType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? compareWith = null)
    {
        try
        {
            var sharedWithPlayerIds = ParseCompareWith(compareWith);
            var performance = await _reportService.GetPerformanceByParAsync(id, courseId, holesPlayed, roundType, startDate, endDate, sharedWithPlayerIds);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance by par for player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the performance by par");
        }
    }

    [HttpGet("{id}/comparison")]
    public async Task<ActionResult<PlayerComparisonResult>> GetPlayerComparison(
        int id,
        [FromQuery] string? compareWith = null,
        [FromQuery] int? courseId = null,
        [FromQuery] int? holesPlayed = null,
        [FromQuery] RoundTypeOption? roundType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var comparePlayerIds = ParseCompareWith(compareWith) ?? new List<int>();

            var result = await _reportService.GetPlayerComparisonAsync(id, comparePlayerIds, courseId, holesPlayed, roundType, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving player comparison for player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the player comparison");
        }
    }

    private static List<int>? ParseCompareWith(string? compareWith)
    {
        if (string.IsNullOrEmpty(compareWith))
            return null;

        var ids = compareWith.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var val) ? val : 0)
            .Where(v => v > 0)
            .ToList();

        return ids.Any() ? ids : null;
    }

    [HttpGet("{id}/rounds/{roundId}/detail")]
    public async Task<ActionResult<RoundDetailSummary>> GetRoundDetail(int id, int roundId)
    {
        try
        {
            var detail = await _reportService.GetRoundDetailAsync(roundId, id);
            if (detail == null)
                return NotFound($"Round {roundId} not found");
            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving round detail {RoundId} for player {PlayerId}", roundId, id);
            return StatusCode(500, "An error occurred while retrieving the round detail");
        }
    }

    // ── My Profile ──

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<Player>> GetMyProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (player == null)
                return NotFound("No player profile found for current user");
            return Ok(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user's profile");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id}/quick-stats")]
    [Authorize]
    public async Task<ActionResult<PlayerQuickStats>> GetPlayerQuickStats(int id)
    {
        try
        {
            var stats = await _reportService.GetPlayerQuickStatsAsync(id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quick stats for player {PlayerId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    // ── Connections ──

    [HttpGet("connections")]
    [Authorize]
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

    [HttpGet("connections/pending-received")]
    [Authorize]
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

    [HttpGet("connections/pending-sent")]
    [Authorize]
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

    [HttpGet("connections/search")]
    [Authorize]
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

    [HttpPost("connections/request")]
    [Authorize]
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

    [HttpPost("connections/{connectionId}/accept")]
    [Authorize]
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

    [HttpPost("connections/{connectionId}/decline")]
    [Authorize]
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

    [HttpDelete("connections/{connectionId}")]
    [Authorize]
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

    // ── Merge ──

    [HttpPost("merge/request")]
    [Authorize]
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

    [HttpGet("merge/pending-received")]
    [Authorize]
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

    [HttpGet("merge/pending-sent")]
    [Authorize]
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

    [HttpPost("merge/{mergeId}/accept")]
    [Authorize]
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

    [HttpPost("merge/{mergeId}/decline")]
    [Authorize]
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

    [HttpGet("merge/mergeable/{targetUserId}")]
    [Authorize]
    public async Task<ActionResult<List<Player>>> GetMergeablePlayers(string targetUserId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var players = await _mergeService.GetMergeablePlayers(userId, targetUserId);
            return Ok(players);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mergeable players");
            return StatusCode(500, "An error occurred");
        }
    }
}

// ── DTOs for API ──

public class ConnectionDto
{
    public int Id { get; set; }
    public string ConnectedUserId { get; set; } = string.Empty;
    public string ConnectedUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ConnectedSince { get; set; }
    public DateTime? RequestedAt { get; set; }
}

public class ConnectionRequestDto
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class MergeRequestDto
{
    public int SourcePlayerId { get; set; }
    public string TargetUserId { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class MergeResponseDto
{
    public int Id { get; set; }
    public string? RequestingUserName { get; set; }
    public string? TargetUserName { get; set; }
    public string SourcePlayerName { get; set; } = string.Empty;
    public string TargetPlayerName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
