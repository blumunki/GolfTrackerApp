using Microsoft.AspNetCore.Mvc;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/[controller]")]
public class SocietiesController : BaseApiController
{
    private readonly IGolfSocietyService _societyService;
    private readonly ILogger<SocietiesController> _logger;

    public SocietiesController(IGolfSocietyService societyService, ILogger<SocietiesController> logger)
    {
        _societyService = societyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAllSocieties()
    {
        try
        {
            var societies = await _societyService.GetAllSocietiesAsync();
            var userId = GetCurrentUserId();
            var result = societies.Select(s => new
            {
                s.GolfSocietyId,
                s.Name,
                s.Description,
                s.CreatedAt,
                MemberCount = s.Memberships.Count,
                IsMember = s.Memberships.Any(m => m.UserId == userId)
            }).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving societies");
            return StatusCode(500, "An error occurred while retrieving societies");
        }
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<object>>> GetMySocieties()
    {
        try
        {
            var userId = GetCurrentUserId();
            var societies = await _societyService.GetSocietiesForUserAsync(userId);
            var result = societies.Select(s => new
            {
                s.GolfSocietyId,
                s.Name,
                s.Description,
                s.CreatedAt,
                MemberCount = s.Memberships.Count,
                MyRole = s.Memberships.FirstOrDefault(m => m.UserId == userId)?.Role.ToString()
            }).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user societies");
            return StatusCode(500, "An error occurred while retrieving societies");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetSociety(int id)
    {
        try
        {
            var society = await _societyService.GetSocietyByIdAsync(id);
            if (society == null) return NotFound();

            var userId = GetCurrentUserId();
            var result = new
            {
                society.GolfSocietyId,
                society.Name,
                society.Description,
                society.CreatedAt,
                CreatedBy = society.CreatedByUser?.UserName,
                Members = society.Memberships.Select(m => new
                {
                    m.UserId,
                    UserName = m.User?.UserName,
                    Role = m.Role.ToString(),
                    m.JoinedAt
                }).ToList(),
                MyRole = society.Memberships.FirstOrDefault(m => m.UserId == userId)?.Role.ToString()
            };
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving society {SocietyId}", id);
            return StatusCode(500, "An error occurred while retrieving the society");
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateSociety([FromBody] CreateSocietyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var society = new GolfSociety
            {
                Name = request.Name,
                Description = request.Description
            };
            var created = await _societyService.CreateSocietyAsync(society, userId);
            return CreatedAtAction(nameof(GetSociety), new { id = created.GolfSocietyId }, new
            {
                created.GolfSocietyId,
                created.Name,
                created.Description,
                created.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating society");
            return StatusCode(500, "An error occurred while creating the society");
        }
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult> JoinSociety(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var membership = await _societyService.JoinSocietyAsync(id, userId);
            return Ok(new { membership.SocietyMembershipId, Role = membership.Role.ToString(), membership.JoinedAt });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining society {SocietyId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost("{id}/leave")]
    public async Task<ActionResult> LeaveSociety(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _societyService.LeaveSocietyAsync(id, userId);
            if (!result) return NotFound();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving society {SocietyId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSociety(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _societyService.DeleteSocietyAsync(id, userId);
            if (!result) return Forbid();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting society {SocietyId}", id);
            return StatusCode(500, "An error occurred");
        }
    }
}

public class CreateSocietyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
