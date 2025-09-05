using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using System.Security.Claims;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GolfCoursesController : ControllerBase
{
    private readonly IGolfCourseService _golfCourseService;
    private readonly ILogger<GolfCoursesController> _logger;

    public GolfCoursesController(IGolfCourseService golfCourseService, ILogger<GolfCoursesController> logger)
    {
        _golfCourseService = golfCourseService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<GolfCourse>>> GetAllGolfCourses()
    {
        try
        {
            var courses = await _golfCourseService.GetAllGolfCoursesAsync();
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving golf courses");
            return StatusCode(500, "An error occurred while retrieving golf courses");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GolfCourse>> GetGolfCourse(int id)
    {
        try
        {
            var course = await _golfCourseService.GetGolfCourseByIdAsync(id);
            if (course == null)
            {
                return NotFound($"Golf course with ID {id} not found");
            }
            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving golf course {CourseId}", id);
            return StatusCode(500, "An error occurred while retrieving the golf course");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<GolfCourse>>> SearchGolfCourses([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var courses = await _golfCourseService.SearchGolfCoursesAsync(searchTerm);
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching golf courses with term: {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching golf courses");
        }
    }

    [HttpPost]
    [Authorize] // Require authentication for creating courses
    public async Task<ActionResult<GolfCourse>> CreateGolfCourse([FromBody] GolfCourse golfCourse)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdCourse = await _golfCourseService.AddGolfCourseAsync(golfCourse);
            return CreatedAtAction(nameof(GetGolfCourse), new { id = createdCourse.GolfCourseId }, createdCourse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating golf course");
            return StatusCode(500, "An error occurred while creating the golf course");
        }
    }

    [HttpPut("{id}")]
    [Authorize] // Require authentication for updating courses
    public async Task<ActionResult<GolfCourse>> UpdateGolfCourse(int id, [FromBody] GolfCourse golfCourse)
    {
        try
        {
            if (id != golfCourse.GolfCourseId)
            {
                return BadRequest("Course ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedCourse = await _golfCourseService.UpdateGolfCourseAsync(golfCourse);
            if (updatedCourse == null)
            {
                return NotFound($"Golf course with ID {id} not found");
            }

            return Ok(updatedCourse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating golf course {CourseId}", id);
            return StatusCode(500, "An error occurred while updating the golf course");
        }
    }

    [HttpDelete("{id}")]
    [Authorize] // Require authentication for deleting courses
    public async Task<ActionResult> DeleteGolfCourse(int id)
    {
        try
        {
            var result = await _golfCourseService.DeleteGolfCourseAsync(id);
            if (!result)
            {
                return NotFound($"Golf course with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting golf course {CourseId}", id);
            return StatusCode(500, "An error occurred while deleting the golf course");
        }
    }
}
