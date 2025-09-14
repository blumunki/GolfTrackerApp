using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoundsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoundsController> _logger;

    public RoundsController(ApplicationDbContext context, ILogger<RoundsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAllRounds()
    {
        try
        {
            var rounds = await _context.Rounds
                .Include(r => r.GolfCourse)
                .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.RoundPlayers)
                .ThenInclude(rp => rp!.Player)
                .Include(r => r.Scores)
                .OrderByDescending(r => r.DatePlayed)
                .Select(r => new
                {
                    RoundId = r.RoundId,
                    CourseName = r.GolfCourse != null ? r.GolfCourse.Name : "Unknown",
                    ClubName = r.GolfCourse != null && r.GolfCourse.GolfClub != null ? r.GolfCourse.GolfClub.Name : "Unknown",
                    DatePlayed = r.DatePlayed,
                    TotalScore = r.Scores.Sum(s => s.Strokes),
                    TotalPar = r.GolfCourse != null ? r.GolfCourse.DefaultPar : 72,
                    HolesPlayed = r.GolfCourse != null ? r.GolfCourse.NumberOfHoles : 18,
                    PlayerCount = r.RoundPlayers.Count,
                    RoundType = r.RoundType.ToString(),
                    PlayingPartners = r.RoundPlayers.Select(rp => rp.Player != null ? $"{rp.Player.FirstName} {rp.Player.LastName}" : "Unknown").ToList(),
                    Weather = "",
                    Notes = r.Notes ?? ""
                })
                .ToListAsync();
            
            return Ok(rounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rounds");
            return StatusCode(500, "An error occurred while retrieving rounds");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Round>> GetRound(int id)
    {
        try
        {
            var round = await _context.Rounds
                .Include(r => r.GolfCourse)
                .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.RoundPlayers)
                .ThenInclude(rp => rp!.Player)
                .Include(r => r.Scores)
                .FirstOrDefaultAsync(r => r.RoundId == id);
                
            if (round == null)
            {
                return NotFound($"Round with ID {id} not found");
            }
            return Ok(round);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving round {RoundId}", id);
            return StatusCode(500, "An error occurred while retrieving the round");
        }
    }

    [HttpGet("{id}/scores")]
    public async Task<ActionResult<List<Score>>> GetRoundScores(int id)
    {
        try
        {
            var scores = await _context.Scores
                .Include(s => s.Hole)
                .Where(s => s.RoundId == id)
                .OrderBy(s => s.Hole != null ? s.Hole.HoleNumber : 1)
                .ToListAsync();
            
            return Ok(scores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scores for round {RoundId}", id);
            return StatusCode(500, "An error occurred while retrieving round scores");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Round>> CreateRound([FromBody] Round round)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Rounds.Add(round);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetRound), new { id = round.RoundId }, round);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating round");
            return StatusCode(500, "An error occurred while creating the round");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<Round>> UpdateRound(int id, [FromBody] Round round)
    {
        try
        {
            if (id != round.RoundId)
            {
                return BadRequest("Round ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(round).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return Ok(round);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Rounds.AnyAsync(r => r.RoundId == id))
            {
                return NotFound($"Round with ID {id} not found");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating round {RoundId}", id);
            return StatusCode(500, "An error occurred while updating the round");
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteRound(int id)
    {
        try
        {
            var round = await _context.Rounds.FindAsync(id);
            if (round == null)
            {
                return NotFound($"Round with ID {id} not found");
            }

            _context.Rounds.Remove(round);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting round {RoundId}", id);
            return StatusCode(500, "An error occurred while deleting the round");
        }
    }
}
