using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public class CourseHole
{
    [System.Text.Json.Serialization.JsonPropertyName("holeId")]
    public int HoleId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("holeNumber")]
    public int HoleNumber { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("par")]
    public int Par { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("strokeIndex")]
    public int StrokeIndex { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("lengthYards")]
    public int? LengthYards { get; set; }
}

public class GolfCourseDetailResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("golfCourseId")]
    public int GolfCourseId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("golfClubId")]
    public int GolfClubId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("defaultPar")]
    public int DefaultPar { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("numberOfHoles")]
    public int NumberOfHoles { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("golfClub")]
    public GolfClub? GolfClub { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("holes")]
    public List<CourseHole> Holes { get; set; } = new();
}

public interface IGolfCourseApiService
{
    Task<List<GolfCourse>> GetAllGolfCoursesAsync();
    Task<GolfCourseDetailResponse?> GetGolfCourseByIdAsync(int id);
    Task<List<GolfCourse>> SearchGolfCoursesAsync(string searchTerm);
    Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId);
}

public class GolfCourseApiService : IGolfCourseApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GolfCourseApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public GolfCourseApiService(
        HttpClient httpClient, 
        ILogger<GolfCourseApiService> logger,
        AuthenticationStateService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private void EnsureAuthorizationHeader()
    {
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    public async Task<List<GolfCourse>> GetAllGolfCoursesAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/golfcourses");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<GolfCourse>>(json, _jsonOptions);
            
            return courses ?? new List<GolfCourse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching golf courses from API");
            return new List<GolfCourse>();
        }
    }

    public async Task<GolfCourseDetailResponse?> GetGolfCourseByIdAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/golfcourses/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var course = JsonSerializer.Deserialize<GolfCourseDetailResponse>(json, _jsonOptions);
            
            return course;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching golf course {CourseId} from API", id);
            return null;
        }
    }

    public async Task<List<GolfCourse>> SearchGolfCoursesAsync(string searchTerm)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/golfcourses/search?searchTerm={Uri.EscapeDataString(searchTerm)}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<GolfCourse>>(json, _jsonOptions);
            
            return courses ?? new List<GolfCourse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching golf courses with term '{SearchTerm}' from API", searchTerm);
            return new List<GolfCourse>();
        }
    }

    public async Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId)
    {
        try
        {
            EnsureAuthorizationHeader();
            // Use the new golf courses byclub endpoint to avoid circular references
            var response = await _httpClient.GetAsync($"api/golfcourses/byclub/{clubId}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<GolfCourse>>(json, _jsonOptions);
            
            return courses ?? new List<GolfCourse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses for club {ClubId} from API", clubId);
            return new List<GolfCourse>();
        }
    }
}
