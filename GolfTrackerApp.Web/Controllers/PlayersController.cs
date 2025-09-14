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

    public PlayersController(ApplicationDbContext context, ILogger<PlayersController> logger, IReportService reportService)
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
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var report = await _reportService.GetPlayerReportViewModelAsync(id, courseId, holesPlayed, roundType, startDate, endDate);
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
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var distribution = await _reportService.GetScoringDistributionAsync(id, courseId, holesPlayed, roundType, startDate, endDate);
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
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var performance = await _reportService.GetPerformanceByParAsync(id, courseId, holesPlayed, roundType, startDate, endDate);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance by par for player {PlayerId}", id);
            return StatusCode(500, "An error occurred while retrieving the performance by par");
        }
    }
}
