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
    public int CoursesPlayed { get; set; }
    public int UniqueCoursesPlayed { get; set; }
    public int UniqueClubsVisited { get; set; }
    public int NineHoleRounds { get; set; }
    public int EighteenHoleRounds { get; set; }
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

public class CourseHistoryItem
{
    public int GolfCourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public DateTime LastPlayedDate { get; set; }
    public int MostRecentScore { get; set; }
    public int MostRecentToPar { get; set; }
    public int BestScore { get; set; }
    public int BestToPar { get; set; }
    public int TimesPlayed { get; set; }
}

public class ScoringDistributionData
{
    public int EagleCount { get; set; }
    public int BirdieCount { get; set; }
    public int ParCount { get; set; }
    public int BogeyCount { get; set; }
    public int DoubleBogeyCount { get; set; }
    public int TripleBogeyOrWorseCount { get; set; }
    public int TotalHoles => EagleCount + BirdieCount + ParCount + BogeyCount + DoubleBogeyCount + TripleBogeyOrWorseCount;
    public double EaglePercentage => TotalHoles > 0 ? (double)EagleCount / TotalHoles * 100 : 0;
    public double BirdiePercentage => TotalHoles > 0 ? (double)BirdieCount / TotalHoles * 100 : 0;
    public double ParPercentage => TotalHoles > 0 ? (double)ParCount / TotalHoles * 100 : 0;
    public double BogeyPercentage => TotalHoles > 0 ? (double)BogeyCount / TotalHoles * 100 : 0;
    public double DoubleBogeyPercentage => TotalHoles > 0 ? (double)DoubleBogeyCount / TotalHoles * 100 : 0;
    public double TripleBogeyOrWorsePercentage => TotalHoles > 0 ? (double)TripleBogeyOrWorseCount / TotalHoles * 100 : 0;
}

public class PerformanceByParData
{
    public double Par3Average { get; set; }
    public double Par4Average { get; set; }
    public double Par5Average { get; set; }
    public int Par3Count { get; set; }
    public int Par4Count { get; set; }
    public int Par5Count { get; set; }
    public double Par3RelativeToPar => Par3Average - 3;
    public double Par4RelativeToPar => Par4Average - 4;
    public double Par5RelativeToPar => Par5Average - 5;
    public bool HasValidData => Par3Count > 0 || Par4Count > 0 || Par5Count > 0;
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
    Task<List<CourseHistoryItem>> GetCourseHistoryAsync(int limit = 6);
    Task<ScoringDistributionData?> GetScoringDistributionDataAsync();
    Task<PerformanceByParData?> GetPerformanceByParAsync();
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
            
            EnsureAuthorizationHeader();
            
            
            _logger.LogInformation("Fetching dashboard stats from Reports API");
            var response = await _httpClient.GetAsync("api/reports/dashboard-stats");
            
            
            _logger.LogInformation($"Dashboard stats API response: {response.StatusCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Dashboard stats API returned 401 Unauthorized");
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Dashboard stats API response content length: {json.Length}");
            
            
            var stats = JsonSerializer.Deserialize<DashboardStats>(json, _jsonOptions);
            
            _logger.LogInformation($"Deserialized dashboard stats: TotalRounds={stats?.TotalRounds ?? 0}");
            
            return stats;
        }
        catch (Exception ex)
        {
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

    public async Task<List<CourseHistoryItem>> GetCourseHistoryAsync(int limit = 6)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/reports/course-history?limit={limit}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CourseHistoryItem>>(json, _jsonOptions) ?? new List<CourseHistoryItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course history from API");
            return new List<CourseHistoryItem>();
        }
    }

    public async Task<ScoringDistributionData?> GetScoringDistributionDataAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/reports/scoring-distribution");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ScoringDistributionData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scoring distribution from API");
            return null;
        }
    }

    public async Task<PerformanceByParData?> GetPerformanceByParAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/reports/performance-by-par");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PerformanceByParData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance by par from API");
            return null;
        }
    }
}
