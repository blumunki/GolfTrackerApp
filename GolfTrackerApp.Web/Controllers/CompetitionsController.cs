using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Models.Api;
using GolfTrackerApp.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GolfTrackerApp.Web.Controllers;

/// <summary>
/// Competitions API. Reads are open to any authenticated user; create/manage is gated by
/// the host authorization model (ARCHITECTURE §12.5 3.5): global Admin always, society
/// Owner/Admin for their society, and granted club Admin for their club.
/// </summary>
[Route("api/[controller]")]
public class CompetitionsController : BaseApiController
{
    private readonly ICompetitionService _competitionService;
    private readonly ILogger<CompetitionsController> _logger;

    public CompetitionsController(
        ICompetitionService competitionService,
        ILogger<CompetitionsController> logger)
    {
        _competitionService = competitionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompetitionSummaryDto>>> GetCompetitions(
        [FromQuery] int? clubId, [FromQuery] int? societyId, [FromQuery] string? status)
    {
        try
        {
            CompetitionStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse<CompetitionStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest($"Unknown status '{status}'.");
                statusFilter = parsed;
            }

            var competitions = await _competitionService.GetCompetitionsAsync(clubId, societyId, statusFilter);
            return Ok(competitions.Select(ToSummary).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing competitions");
            return StatusCode(500, "An error occurred while retrieving competitions");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompetitionDetailDto>> GetCompetition(int id)
    {
        try
        {
            var competition = await _competitionService.GetCompetitionByIdAsync(id);
            if (competition is null) return NotFound($"Competition with ID {id} not found");

            var dto = new CompetitionDetailDto
            {
                Description = competition.Description,
                Entries = competition.Entries.Select(e => new CompetitionEntryDto
                {
                    CompetitionEntryId = e.CompetitionEntryId,
                    PlayerId = e.PlayerId,
                    PlayerName = $"{e.Player?.FirstName} {e.Player?.LastName}".Trim(),
                    TeeSetName = e.TeeSet?.Name,
                    HandicapAtEntry = e.HandicapAtEntry,
                    GrossScore = e.GrossScore,
                    NetScore = e.NetScore,
                    StablefordPoints = e.StablefordPoints,
                    Position = e.Position,
                }).ToList(),
                LinkedRoundIds = competition.Rounds.Select(r => r.RoundId).ToList(),
            };
            CopySummary(competition, dto);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while retrieving the competition");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CompetitionSummaryDto>> CreateCompetition([FromBody] CreateCompetitionRequest request)
    {
        try
        {
            if (!Enum.TryParse<ScoringFormat>(request.ScoringFormat, ignoreCase: true, out var format))
                return BadRequest($"Unknown scoring format '{request.ScoringFormat}'.");
            if (!await CanManageHostAsync(request.GolfClubId, request.GolfSocietyId))
                return Forbid();

            var created = await _competitionService.CreateCompetitionAsync(new Competition
            {
                Name = request.Name,
                GolfClubId = request.GolfClubId,
                GolfSocietyId = request.GolfSocietyId,
                GolfCourseId = request.GolfCourseId,
                ScoringFormat = format,
                Date = request.Date,
                Description = request.Description,
                IsOpen = request.IsOpen,
                CreatedByUserId = GetCurrentUserId(),
            });
            return CreatedAtAction(nameof(GetCompetition), new { id = created.CompetitionId }, ToSummary(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating competition");
            return StatusCode(500, "An error occurred while creating the competition");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CompetitionSummaryDto>> UpdateCompetition(int id, [FromBody] UpdateCompetitionRequest request)
    {
        try
        {
            var existing = await _competitionService.GetCompetitionByIdAsync(id);
            if (existing is null) return NotFound($"Competition with ID {id} not found");
            if (!await CanManageHostAsync(existing.GolfClubId, existing.GolfSocietyId))
                return Forbid();
            if (!Enum.TryParse<ScoringFormat>(request.ScoringFormat, ignoreCase: true, out var format))
                return BadRequest($"Unknown scoring format '{request.ScoringFormat}'.");

            var updated = await _competitionService.UpdateCompetitionAsync(new Competition
            {
                CompetitionId = id,
                Name = request.Name,
                GolfCourseId = request.GolfCourseId,
                ScoringFormat = format,
                Date = request.Date,
                Description = request.Description,
                IsOpen = request.IsOpen,
            });
            return updated is null
                ? NotFound($"Competition with ID {id} not found")
                : Ok(ToSummary(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while updating the competition");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<CompetitionSummaryDto>> SetStatus(int id, [FromBody] CompetitionStatusRequest request)
    {
        try
        {
            if (!Enum.TryParse<CompetitionStatus>(request.Status, ignoreCase: true, out var status))
                return BadRequest($"Unknown status '{request.Status}'.");

            var existing = await _competitionService.GetCompetitionByIdAsync(id);
            if (existing is null) return NotFound($"Competition with ID {id} not found");
            if (!await CanManageHostAsync(existing.GolfClubId, existing.GolfSocietyId))
                return Forbid();

            var updated = await _competitionService.SetStatusAsync(id, status);
            if (updated is not null && status == CompetitionStatus.Completed)
            {
                await _competitionService.ComputeResultsAsync(id);
                updated = await _competitionService.GetCompetitionByIdAsync(id);
            }

            return updated is null
                ? NotFound($"Competition with ID {id} not found")
                : Ok(ToSummary(updated));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting status for competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while updating the competition status");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var existing = await _competitionService.GetCompetitionByIdAsync(id);
            if (existing is null) return NotFound($"Competition with ID {id} not found");
            if (!await CanManageHostAsync(existing.GolfClubId, existing.GolfSocietyId))
                return Forbid();

            await _competitionService.DeleteCompetitionAsync(id);
            return NoContent(); // rounds are unlinked, never deleted
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while deleting the competition");
        }
    }

    [HttpPost("{id}/entries")]
    public async Task<ActionResult> AddEntry(int id, [FromBody] CompetitionEntryRequest request)
    {
        try
        {
            var competition = await _competitionService.GetCompetitionByIdAsync(id);
            if (competition is null) return NotFound($"Competition with ID {id} not found");

            CompetitionEntry entry;
            if (await CanManageHostAsync(competition.GolfClubId, competition.GolfSocietyId))
            {
                entry = await _competitionService.AddEntryAsync(id, request.PlayerId, request.TeeSetId);
            }
            else
            {
                var registration = await _competitionService.GetRegistrationStatusAsync(
                    id, GetCurrentUserId());
                if (registration?.PlayerId != request.PlayerId) return Forbid();
                entry = await _competitionService.RegisterUserAsync(
                    id, GetCurrentUserId(), request.TeeSetId);
            }

            return Ok(new { entry.CompetitionEntryId, entry.CompetitionId, entry.PlayerId, entry.HandicapAtEntry });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entry to competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while entering the competition");
        }
    }

    [HttpDelete("{id}/entries/{playerId}")]
    public async Task<ActionResult> RemoveEntry(int id, int playerId)
    {
        try
        {
            var competition = await _competitionService.GetCompetitionByIdAsync(id);
            if (competition is null) return NotFound($"Competition with ID {id} not found");

            bool removed;
            if (await CanManageHostAsync(competition.GolfClubId, competition.GolfSocietyId))
            {
                removed = await _competitionService.RemoveEntryAsync(id, playerId);
            }
            else
            {
                var registration = await _competitionService.GetRegistrationStatusAsync(
                    id, GetCurrentUserId());
                if (registration?.PlayerId != playerId) return Forbid();
                removed = await _competitionService.WithdrawUserAsync(id, GetCurrentUserId());
            }

            return removed ? NoContent() : NotFound("No such entry");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing entry from competition {CompetitionId}", id);
            return StatusCode(500, "An error occurred while withdrawing from the competition");
        }
    }

    [HttpGet("{id}/registration")]
    public async Task<ActionResult<CompetitionRegistrationStatus>> GetRegistration(int id)
    {
        var status = await _competitionService.GetRegistrationStatusAsync(id, GetCurrentUserId());
        return status is null
            ? NotFound($"Competition with ID {id} not found")
            : Ok(status);
    }

    [HttpPost("{id}/registration")]
    public async Task<ActionResult<CompetitionRegistrationStatus>> Register(
        int id,
        [FromBody] CompetitionRegistrationRequest request)
    {
        try
        {
            await _competitionService.RegisterUserAsync(id, GetCurrentUserId(), request.TeeSetId);
            return Ok(await _competitionService.GetRegistrationStatusAsync(id, GetCurrentUserId()));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}/registration")]
    public async Task<ActionResult> WithdrawRegistration(int id)
    {
        try
        {
            var removed = await _competitionService.WithdrawUserAsync(id, GetCurrentUserId());
            return removed ? NoContent() : NotFound("No registration found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id}/rounds/{roundId}")]
    public async Task<ActionResult> AssignRound(int id, int roundId)
    {
        try
        {
            // Ownership (round belongs to caller unless admin) is enforced by the service.
            var round = await _competitionService.AssignRoundAsync(
                roundId, id, GetCurrentUserId(), User.IsInRole("Admin"));
            return round is null ? NotFound("Round or competition not found") : Ok(new { round.RoundId, round.CompetitionId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning round {RoundId} to competition {CompetitionId}", roundId, id);
            return StatusCode(500, "An error occurred while assigning the round");
        }
    }

    [HttpDelete("rounds/{roundId}")]
    public async Task<ActionResult> UnassignRound(int roundId)
    {
        try
        {
            var round = await _competitionService.UnassignRoundAsync(
                roundId, GetCurrentUserId(), User.IsInRole("Admin"));
            return round is null ? NotFound("Round not found") : Ok(new { round.RoundId, round.CompetitionId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning round {RoundId}", roundId);
            return StatusCode(500, "An error occurred while unassigning the round");
        }
    }

    /// <summary>Host gate (§12.5 3.5): global Admin always; otherwise defer to the host-manager check.</summary>
    private async Task<bool> CanManageHostAsync(int? golfClubId, int? golfSocietyId)
    {
        return User.IsInRole("Admin")
            || await _competitionService.IsHostManagerAsync(golfClubId, golfSocietyId, GetCurrentUserId());
    }

    private static CompetitionSummaryDto ToSummary(Competition competition)
    {
        var dto = new CompetitionSummaryDto();
        CopySummary(competition, dto);
        return dto;
    }

    private static void CopySummary(Competition competition, CompetitionSummaryDto dto)
    {
        dto.CompetitionId = competition.CompetitionId;
        dto.Name = competition.Name;
        dto.GolfClubId = competition.GolfClubId;
        dto.GolfClubName = competition.GolfClub?.Name;
        dto.GolfSocietyId = competition.GolfSocietyId;
        dto.GolfSocietyName = competition.GolfSociety?.Name;
        dto.GolfCourseId = competition.GolfCourseId;
        dto.GolfCourseName = competition.GolfCourse?.Name;
        dto.ScoringFormat = competition.ScoringFormat.ToString();
        dto.Date = competition.Date;
        dto.Status = competition.Status.ToString();
        dto.IsOpen = competition.IsOpen;
        dto.EntryCount = competition.Entries.Count;
    }
}
