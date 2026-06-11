using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Models.Api;
using GolfTrackerApp.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Controllers;

[Route("api/handicaps")]
public class HandicapsController : BaseApiController
{
    private readonly IHandicapService _handicapService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HandicapsController> _logger;

    public HandicapsController(
        IHandicapService handicapService,
        ApplicationDbContext context,
        ILogger<HandicapsController> logger)
    {
        _handicapService = handicapService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("players/{playerId}/records")]
    public async Task<ActionResult<List<HandicapRecordDto>>> GetRecords(int playerId, [FromQuery] string? source = null)
    {
        try
        {
            if (!await CanAccessPlayerAsync(playerId)) return Forbid();

            HandicapSource? sourceFilter = null;
            if (source is not null)
            {
                if (!Enum.TryParse<HandicapSource>(source, ignoreCase: true, out var parsed))
                    return BadRequest($"Unknown handicap source '{source}'.");
                sourceFilter = parsed;
            }

            var records = await _handicapService.GetHandicapRecordsAsync(playerId, sourceFilter);
            return Ok(records.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving handicap records for player {PlayerId}", playerId);
            return StatusCode(500, "An error occurred while retrieving handicap records");
        }
    }

    [HttpGet("players/{playerId}/active")]
    public async Task<ActionResult<List<HandicapRecordDto>>> GetActive(int playerId)
    {
        try
        {
            if (!await CanAccessPlayerAsync(playerId)) return Forbid();

            var records = await _handicapService.GetActiveHandicapsAsync(playerId);
            return Ok(records.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active handicaps for player {PlayerId}", playerId);
            return StatusCode(500, "An error occurred while retrieving active handicaps");
        }
    }

    [HttpGet("players/{playerId}/differentials")]
    public async Task<ActionResult<List<ScoringDifferentialDto>>> GetDifferentials(int playerId)
    {
        try
        {
            if (!await CanAccessPlayerAsync(playerId)) return Forbid();

            var differentials = await _handicapService.GetRecentDifferentialsAsync(playerId);
            return Ok(differentials.Select(d => new ScoringDifferentialDto
            {
                RoundId = d.RoundId,
                DatePlayed = d.Round?.DatePlayed ?? default,
                CourseName = d.Round?.GolfCourse?.Name,
                TeeName = d.TeeSet?.Name,
                AdjustedGrossScore = d.AdjustedGrossScore,
                CourseRating = d.CourseRating,
                SlopeRating = d.SlopeRating,
                Differential = d.Differential,
                IsUsedInCalculation = d.IsUsedInCalculation,
            }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving differentials for player {PlayerId}", playerId);
            return StatusCode(500, "An error occurred while retrieving scoring differentials");
        }
    }

    [HttpPost("club")]
    public async Task<ActionResult<HandicapRecordDto>> CreateClubHandicap([FromBody] ClubHandicapRequestDto request)
    {
        try
        {
            if (!await CanAccessPlayerAsync(request.PlayerId)) return Forbid();

            var added = await _handicapService.AddManualClubHandicapAsync(new HandicapRecord
            {
                PlayerId = request.PlayerId,
                GolfClubId = request.GolfClubId,
                HandicapIndex = request.HandicapIndex,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
            });
            return Ok(ToDto(added));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating club handicap for player {PlayerId}", request.PlayerId);
            return StatusCode(500, "An error occurred while creating the club handicap");
        }
    }

    [HttpPut("club/{id}")]
    public async Task<ActionResult<HandicapRecordDto>> UpdateClubHandicap(int id, [FromBody] ClubHandicapRequestDto request)
    {
        try
        {
            if (!await CanAccessRecordAsync(id)) return Forbid();

            var updated = await _handicapService.UpdateManualClubHandicapAsync(new HandicapRecord
            {
                HandicapRecordId = id,
                PlayerId = request.PlayerId,
                GolfClubId = request.GolfClubId,
                HandicapIndex = request.HandicapIndex,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
            });
            if (updated is null) return NotFound($"Handicap record {id} not found");
            return Ok(ToDto(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // calculated record — not editable
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating club handicap {RecordId}", id);
            return StatusCode(500, "An error occurred while updating the club handicap");
        }
    }

    [HttpDelete("club/{id}")]
    public async Task<ActionResult> DeleteClubHandicap(int id)
    {
        try
        {
            if (!await CanAccessRecordAsync(id)) return Forbid();

            if (!await _handicapService.DeleteManualClubHandicapAsync(id))
                return NotFound($"Handicap record {id} not found");
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // calculated record — not deletable
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting club handicap {RecordId}", id);
            return StatusCode(500, "An error occurred while deleting the club handicap");
        }
    }

    /// <summary>Owner-or-self check, matching the PlayersController access rule.</summary>
    private async Task<bool> CanAccessPlayerAsync(int playerId)
    {
        var userId = GetCurrentUserId();
        return await _context.Players.AnyAsync(p => p.PlayerId == playerId
            && (p.ApplicationUserId == userId || p.CreatedByApplicationUserId == userId));
    }

    private async Task<bool> CanAccessRecordAsync(int handicapRecordId)
    {
        var playerId = await _context.HandicapRecords
            .Where(h => h.HandicapRecordId == handicapRecordId)
            .Select(h => (int?)h.PlayerId)
            .FirstOrDefaultAsync();
        // Missing records fall through to the service's not-found result.
        return playerId is null || await CanAccessPlayerAsync(playerId.Value);
    }

    private static HandicapRecordDto ToDto(HandicapRecord record) => new()
    {
        Id = record.HandicapRecordId,
        PlayerId = record.PlayerId,
        HandicapIndex = record.HandicapIndex,
        Source = record.Source.ToString(),
        GolfClubId = record.GolfClubId,
        GolfClubName = record.GolfClub?.Name,
        GolfSocietyId = record.GolfSocietyId,
        GolfSocietyName = record.GolfSociety?.Name,
        EffectiveDate = record.EffectiveDate,
        ExpiryDate = record.ExpiryDate,
        IsManualEntry = record.IsManualEntry,
    };
}
