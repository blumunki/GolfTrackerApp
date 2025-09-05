using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using System.Security.Claims;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GolfClubsController : ControllerBase
{
    private readonly IGolfClubService _golfClubService;
    private readonly ILogger<GolfClubsController> _logger;

    public GolfClubsController(IGolfClubService golfClubService, ILogger<GolfClubsController> logger)
    {
        _golfClubService = golfClubService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<GolfClub>>> GetAllGolfClubs()
    {
        try
        {
            var clubs = await _golfClubService.GetAllGolfClubsAsync();
            return Ok(clubs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving golf clubs");
            return StatusCode(500, "An error occurred while retrieving golf clubs");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GolfClub>> GetGolfClub(int id)
    {
        try
        {
            var club = await _golfClubService.GetGolfClubByIdAsync(id);
            if (club == null)
            {
                return NotFound($"Golf club with ID {id} not found");
            }
            return Ok(club);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving golf club {ClubId}", id);
            return StatusCode(500, "An error occurred while retrieving the golf club");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<GolfClub>>> SearchGolfClubs([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var clubs = await _golfClubService.SearchGolfClubsAsync(searchTerm);
            return Ok(clubs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching golf clubs with term: {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching golf clubs");
        }
    }

    [HttpPost]
    [Authorize] // Require authentication for creating clubs
    public async Task<ActionResult<GolfClub>> CreateGolfClub([FromBody] GolfClub golfClub)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdClub = await _golfClubService.AddGolfClubAsync(golfClub);
            return CreatedAtAction(nameof(GetGolfClub), new { id = createdClub.GolfClubId }, createdClub);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating golf club");
            return StatusCode(500, "An error occurred while creating the golf club");
        }
    }

    [HttpPut("{id}")]
    [Authorize] // Require authentication for updating clubs
    public async Task<ActionResult<GolfClub>> UpdateGolfClub(int id, [FromBody] GolfClub golfClub)
    {
        try
        {
            if (id != golfClub.GolfClubId)
            {
                return BadRequest("Club ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedClub = await _golfClubService.UpdateGolfClubAsync(golfClub);
            if (updatedClub == null)
            {
                return NotFound($"Golf club with ID {id} not found");
            }

            return Ok(updatedClub);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating golf club {ClubId}", id);
            return StatusCode(500, "An error occurred while updating the golf club");
        }
    }

    [HttpDelete("{id}")]
    [Authorize] // Require authentication for deleting clubs
    public async Task<ActionResult> DeleteGolfClub(int id)
    {
        try
        {
            var result = await _golfClubService.DeleteGolfClubAsync(id);
            if (!result)
            {
                return NotFound($"Golf club with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting golf club {ClubId}", id);
            return StatusCode(500, "An error occurred while deleting the golf club");
        }
    }
}
