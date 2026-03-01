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

    public PlayersController(
        ApplicationDbContext context, 
        ILogger<PlayersController> logger, 
        IReportService reportService)
    {
        _context = context;
        _logger = logger;
        _reportService = reportService;
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

    [HttpPost("batch-quick-stats")]
    [Authorize]
    public async Task<ActionResult<Dictionary<int, PlayerQuickStats>>> GetBatchQuickStats([FromBody] List<int> playerIds)
    {
        try
        {
            if (playerIds == null || playerIds.Count == 0)
                return Ok(new Dictionary<int, PlayerQuickStats>());

            // Cap at 50 to prevent abuse
            var ids = playerIds.Take(50).ToList();
            var stats = await _reportService.GetBatchPlayerQuickStatsAsync(ids);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch quick stats for {Count} players", playerIds?.Count ?? 0);
            return StatusCode(500, "An error occurred");
        }
    }

}
