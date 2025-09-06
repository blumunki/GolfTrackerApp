using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

// Response models to match the Dashboard API responses
public class DashboardStats
{
    public int TotalRounds { get; set; }
    public decimal AverageScore { get; set; }
    public int BestScore { get; set; }
    public decimal CurrentHandicap { get; set; }
    public int TotalPlayingPartners { get; set; }
}

public class RecentActivity
{
    public int ActivityId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class ScoreDistribution
{
    public string ScoreRange { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class FavoriteCourse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public decimal AverageScore { get; set; }
}

public interface IDashboardApiService
{
    Task<DashboardStats?> GetDashboardStatsAsync();
    Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 5);
    Task<List<ScoreDistribution>> GetScoreDistributionAsync();
    Task<List<FavoriteCourse>> GetFavoriteCoursesAsync(int limit = 5);
}

public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DashboardApiService(HttpClient httpClient, ILogger<DashboardApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/dashboard/stats");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var stats = JsonSerializer.Deserialize<DashboardStats>(json, _jsonOptions);
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats from API");
            return null;
        }
    }

    public async Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/dashboard/recent-activity?limit={limit}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var activity = JsonSerializer.Deserialize<List<RecentActivity>>(json, _jsonOptions);
            
            return activity ?? new List<RecentActivity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activity from API");
            return new List<RecentActivity>();
        }
    }

    public async Task<List<ScoreDistribution>> GetScoreDistributionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/dashboard/score-distribution");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var distribution = JsonSerializer.Deserialize<List<ScoreDistribution>>(json, _jsonOptions);
            
            return distribution ?? new List<ScoreDistribution>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching score distribution from API");
            return new List<ScoreDistribution>();
        }
    }

    public async Task<List<FavoriteCourse>> GetFavoriteCoursesAsync(int limit = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/dashboard/favorite-courses?limit={limit}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var courses = JsonSerializer.Deserialize<List<FavoriteCourse>>(json, _jsonOptions);
            
            return courses ?? new List<FavoriteCourse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching favorite courses from API");
            return new List<FavoriteCourse>();
        }
    }
}
