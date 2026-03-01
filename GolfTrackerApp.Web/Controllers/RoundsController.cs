using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Services;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class RoundsController : BaseApiController
{
    private readonly IRoundService _roundService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoundsController> _logger;

    public RoundsController(IRoundService roundService, ApplicationDbContext context, ILogger<RoundsController> logger)
    {
        _roundService = roundService;
        _context = context;
        _logger = logger;
    }

    private static int ComputeRoundPar(Round r) => RoundService.ComputeRoundPar(r);

    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAllRounds()
    {
        try
        {
            var userId = GetCurrentUserId();
            var currentPlayer = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            var currentPlayerId = currentPlayer?.PlayerId;

            // Only return rounds the user created or participated in
            var query = _context.Rounds
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.Holes)
                .Include(r => r.RoundPlayers)
                    .ThenInclude(rp => rp!.Player)
                .Include(r => r.Scores)
                .AsSplitQuery()
                .Where(r => r.CreatedByApplicationUserId == userId 
                    || (currentPlayerId.HasValue && r.RoundPlayers.Any(rp => rp.PlayerId == currentPlayerId.Value)))
                .OrderByDescending(r => r.DatePlayed);

            var roundEntities = await query.ToListAsync();

            var rounds = roundEntities.Select(r => new
                {
                    RoundId = r.RoundId,
                    CourseName = r.GolfCourse?.Name ?? "Unknown",
                    ClubName = r.GolfCourse?.GolfClub?.Name ?? "Unknown",
                    DatePlayed = r.DatePlayed,
                    TotalScore = currentPlayerId.HasValue 
                        ? r.Scores.Where(s => s.PlayerId == currentPlayerId.Value).Sum(s => s.Strokes)
                        : r.Scores.Sum(s => s.Strokes),
                    TotalPar = ComputeRoundPar(r),
                    HolesPlayed = r.HolesPlayed,
                    PlayerCount = r.RoundPlayers.Count,
                    RoundType = r.RoundType.ToString(),
                    PlayingPartners = r.RoundPlayers.Select(rp => rp.Player != null ? $"{rp.Player.FirstName} {rp.Player.LastName}" : "Unknown").ToList(),
                    Weather = "",
                    Notes = r.Notes ?? ""
                })
                .ToList();
            
            return Ok(rounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rounds");
            return StatusCode(500, "An error occurred while retrieving rounds");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoundResponse>> GetRound(int id)
    {
        try
        {
            // Determine the current user's player ID for per-user score
            var userId = GetCurrentUserId();
            var currentPlayer = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            var currentPlayerId = currentPlayer?.PlayerId;

            var round = await _context.Rounds
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.Holes)
                .Include(r => r.RoundPlayers)
                    .ThenInclude(rp => rp!.Player)
                .Include(r => r.Scores)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.RoundId == id);
                
            if (round == null)
            {
                return NotFound($"Round with ID {id} not found");
            }

            var response = new RoundResponse
            {
                RoundId = round.RoundId,
                GolfCourseId = round.GolfCourseId,
                DatePlayed = round.DatePlayed,
                StartingHole = round.StartingHole,
                HolesPlayed = round.HolesPlayed,
                RoundType = round.RoundType.ToString(),
                Notes = round.Notes,
                Status = round.Status.ToString(),
                CreatedByApplicationUserId = round.CreatedByApplicationUserId,
                CourseName = round.GolfCourse?.Name ?? "Unknown",
                ClubName = round.GolfCourse?.GolfClub?.Name ?? "Unknown",
                TotalScore = currentPlayerId.HasValue
                    ? round.Scores.Where(s => s.PlayerId == currentPlayerId.Value).Sum(s => s.Strokes)
                    : round.Scores.Sum(s => s.Strokes),
                TotalPar = ComputeRoundPar(round),
                PlayerCount = round.RoundPlayers.Count,
                PlayingPartners = round.RoundPlayers
                    .Select(rp => rp.Player != null ? $"{rp.Player.FirstName} {rp.Player.LastName}" : "Unknown")
                    .ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving round {RoundId}", id);
            return StatusCode(500, "An error occurred while retrieving the round");
        }
    }

    [HttpGet("{id}/scores")]
    public async Task<ActionResult<List<object>>> GetRoundScores(int id)
    {
        try
        {
            var scores = await _context.Scores
                .Where(s => s.RoundId == id)
                .OrderBy(s => s.Hole != null ? s.Hole.HoleNumber : 1)
                .Select(s => new
                {
                    ScoreId = s.ScoreId,
                    RoundId = s.RoundId,
                    PlayerId = s.PlayerId,
                    HoleId = s.HoleId,
                    Strokes = s.Strokes,
                    Putts = s.Putts,
                    FairwayHit = s.FairwayHit,
                    Player = s.Player != null ? new
                    {
                        PlayerId = s.Player.PlayerId,
                        FirstName = s.Player.FirstName,
                        LastName = s.Player.LastName
                    } : null,
                    Hole = s.Hole != null ? new
                    {
                        HoleId = s.Hole.HoleId,
                        HoleNumber = s.Hole.HoleNumber,
                        Par = s.Hole.Par
                    } : null
                })
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
    public async Task<ActionResult<RoundResponse>> CreateRound([FromBody] Round round)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Always set the creator from the authenticated user
            round.CreatedByApplicationUserId = GetCurrentUserId();

            _context.Rounds.Add(round);
            await _context.SaveChangesAsync();
            
            // Reload with related data to calculate response
            var savedRound = await _context.Rounds
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.GolfClub)
                .Include(r => r.GolfCourse)
                    .ThenInclude(gc => gc!.Holes)
                .Include(r => r.RoundPlayers)
                    .ThenInclude(rp => rp!.Player)
                .Include(r => r.Scores)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.RoundId == round.RoundId);
            
            if (savedRound == null)
            {
                return StatusCode(500, "Round was saved but could not be retrieved");
            }
            
            // Map to DTO to avoid circular references
            var response = new RoundResponse
            {
                RoundId = savedRound.RoundId,
                GolfCourseId = savedRound.GolfCourseId,
                DatePlayed = savedRound.DatePlayed,
                StartingHole = savedRound.StartingHole,
                HolesPlayed = savedRound.HolesPlayed,
                RoundType = savedRound.RoundType.ToString(),
                Notes = savedRound.Notes,
                Status = savedRound.Status.ToString(),
                CreatedByApplicationUserId = savedRound.CreatedByApplicationUserId,
                CourseName = savedRound.GolfCourse?.Name ?? "Unknown",
                ClubName = savedRound.GolfCourse?.GolfClub?.Name ?? "Unknown",
                TotalScore = savedRound.Scores.Sum(s => s.Strokes),
                TotalPar = ComputeRoundPar(savedRound),
                PlayerCount = savedRound.RoundPlayers.Count,
                PlayingPartners = savedRound.RoundPlayers
                    .Select(rp => rp.Player != null ? $"{rp.Player.FirstName} {rp.Player.LastName}" : "Unknown")
                    .ToList()
            };
            
            return CreatedAtAction(nameof(GetRound), new { id = savedRound.RoundId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating round");
            return StatusCode(500, "An error occurred while creating the round");
        }
    }

    [HttpPut("{id}")]
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

            // Verify ownership
            var existingRound = await _context.Rounds.AsNoTracking().FirstOrDefaultAsync(r => r.RoundId == id);
            if (existingRound == null)
            {
                return NotFound($"Round with ID {id} not found");
            }
            if (existingRound.CreatedByApplicationUserId != GetCurrentUserId())
            {
                return Forbid();
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
    public async Task<ActionResult> DeleteRound(int id)
    {
        try
        {
            var round = await _context.Rounds.FindAsync(id);
            if (round == null)
            {
                return NotFound($"Round with ID {id} not found");
            }

            // Verify ownership
            if (round.CreatedByApplicationUserId != GetCurrentUserId())
            {
                return Forbid();
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
