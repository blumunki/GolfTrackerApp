using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http.Headers;

namespace GolfTrackerApp.Mobile.Services.Api;

// Enhanced models to match the web app ReportService
public class DashboardStats
{
    public int TotalRounds { get; set; }
    public double? AverageScore { get; set; }
    public double? AverageToPar { get; set; }
    public int? BestScore { get; set; }
    public int? LowestToPar { get; set; }
    public string? BestScoreCourseName { get; set; }
    public DateTime? BestScoreDate { get; set; }
    public DateTime? LastRoundDate { get; set; }
    public string? FavoriteCourseName { get; set; }
    public int FavoriteCourseRounds { get; set; }
    public bool IsImprovingStreak { get; set; }
    public int CurrentStreak { get; set; }
    public int CoursesPlayed { get; set; } // Add missing property
}

public class PlayingPartnerSummary
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string PlayerName => PartnerName; // Alias for consistency
    public int RoundsPlayed => UserWins + PartnerWins + Ties; // Calculate total rounds
    public DateTime LastPlayedDate { get; set; }
    public int UserWins { get; set; }
    public int PartnerWins { get; set; }
    public int Ties { get; set; }
}

public class PlayerPerformanceDataPoint
{
    public DateTime Date { get; set; }
    public int ScoreVsPar { get; set; }
}

public class Round
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string? CourseName { get; set; }
    public string? ClubName { get; set; }
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
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
    Task<List<PlayingPartnerSummary>> GetPlayingPartnersAsync(int limit = 5);
    Task<List<PlayerPerformanceDataPoint>> GetPerformanceSummaryAsync(int roundCount = 7);
    Task<List<Round>> GetRecentRoundsAsync(int limit = 5);
    Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 5);
    Task<List<ScoreDistribution>> GetScoreDistributionAsync();
    Task<List<FavoriteCourse>> GetFavoriteCoursesAsync(int limit = 5);
}

public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public DashboardApiService(
        HttpClient httpClient, 
        ILogger<DashboardApiService> logger,
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

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            // Debug: Check if we even enter the service method
            Console.WriteLine("[DASHBOARD_API] GetDashboardStatsAsync() method entered");
            System.Diagnostics.Debug.WriteLine("[DASHBOARD_API] GetDashboardStatsAsync() method entered");
            
            EnsureAuthorizationHeader();
            
            Console.WriteLine($"[DASHBOARD_API] HttpClient.BaseAddress: {_httpClient.BaseAddress}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] HttpClient.BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[DASHBOARD_API] Authorization header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Authorization header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
            
            _logger.LogInformation("Fetching dashboard stats from Reports API");
            var response = await _httpClient.GetAsync("api/reports/dashboard-stats");
            
            Console.WriteLine($"[DASHBOARD_API] Response Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Response Status: {response.StatusCode}");
            
            _logger.LogInformation($"Dashboard stats API response: {response.StatusCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Dashboard stats API returned 401 Unauthorized");
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Dashboard stats API response content length: {json.Length}");
            
            Console.WriteLine($"[DASHBOARD_API] Response JSON: {json}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Response JSON: {json}");
            
            var stats = JsonSerializer.Deserialize<DashboardStats>(json, _jsonOptions);
            
            _logger.LogInformation($"Deserialized dashboard stats: TotalRounds={stats?.TotalRounds ?? 0}");
            Console.WriteLine($"[DASHBOARD_API] Deserialized stats: TotalRounds={stats?.TotalRounds ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Deserialized stats: TotalRounds={stats?.TotalRounds ?? 0}");
            
            return stats;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DASHBOARD_API] Exception: {ex.Message}");
            Console.WriteLine($"[DASHBOARD_API] Exception details: {ex}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DASHBOARD_API] Exception details: {ex}");
            _logger.LogError(ex, "Error fetching dashboard stats from API");
            return null;
        }
    }

    public async Task<List<PlayingPartnerSummary>> GetPlayingPartnersAsync(int limit = 5)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.GetAsync($"api/reports/playing-partners?limit={limit}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var partners = JsonSerializer.Deserialize<List<PlayingPartnerSummary>>(json, _jsonOptions);
            
            return partners ?? new List<PlayingPartnerSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching playing partners from API");
            return new List<PlayingPartnerSummary>();
        }
    }

    public async Task<List<PlayerPerformanceDataPoint>> GetPerformanceSummaryAsync(int roundCount = 7)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.GetAsync($"api/reports/performance-summary?roundCount={roundCount}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var performance = JsonSerializer.Deserialize<List<PlayerPerformanceDataPoint>>(json, _jsonOptions);
            
            return performance ?? new List<PlayerPerformanceDataPoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance summary from API");
            return new List<PlayerPerformanceDataPoint>();
        }
    }

    public async Task<List<Round>> GetRecentRoundsAsync(int limit = 5)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.GetAsync($"api/reports/recent-rounds?limit={limit}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var rounds = JsonSerializer.Deserialize<List<Round>>(json, _jsonOptions);
            
            return rounds ?? new List<Round>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent rounds from API");
            return new List<Round>();
        }
    }

    public async Task<List<RecentActivity>> GetRecentActivityAsync(int limit = 5)
    {
        try
        {
            EnsureAuthorizationHeader();
            
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
            EnsureAuthorizationHeader();
            
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
            EnsureAuthorizationHeader();
            
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
