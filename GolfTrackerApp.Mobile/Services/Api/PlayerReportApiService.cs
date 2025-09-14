using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using GolfTrackerApp.Mobile.Models;

namespace GolfTrackerApp.Mobile.Services.Api;

// Response models for player report data
public class PlayerReportViewModel
{
    public Player? Player { get; set; }
    
    [JsonPropertyName("filterCourses")]
    public List<GolfCourse> FilterCourses { get; set; } = new();
    
    [JsonPropertyName("performanceData")]
    public List<PlayerReportPerformanceDataPoint> PerformanceData { get; set; } = new();
}

public class PlayerReportPerformanceDataPoint
{
    public DateTime Date { get; set; }
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int ScoreVsPar => TotalScore - TotalPar;
    public string CourseName { get; set; } = string.Empty;
    public int HolesPlayed { get; set; }
}

public class ScoringDistribution
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

public class PerformanceByPar
{
    public double Par3Average { get; set; }
    public double Par4Average { get; set; }
    public double Par5Average { get; set; }
    
    public int Par3Count { get; set; }
    public int Par4Count { get; set; }
    public int Par5Count { get; set; }
    
    // Relative to par calculations
    public double Par3RelativeToPar => Par3Average - 3;
    public double Par4RelativeToPar => Par4Average - 4;
    public double Par5RelativeToPar => Par5Average - 5;
    
    public bool HasValidData => Par3Count > 0 || Par4Count > 0 || Par5Count > 0;
}

public enum RoundTypeOption
{
    Friendly,
    Competitive
}

public interface IPlayerReportApiService
{
    Task<PlayerReportViewModel?> GetPlayerReportAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<ScoringDistribution?> GetScoringDistributionAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<PerformanceByPar?> GetPerformanceByParAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null);
    string LastApiError { get; }
}

public class PlayerReportApiService : IPlayerReportApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlayerReportApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public PlayerReportApiService(
        HttpClient httpClient, 
        ILogger<PlayerReportApiService> logger,
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

    private string BuildQueryString(int? courseId, int? holesPlayed, RoundTypeOption? roundType, DateTime? startDate, DateTime? endDate)
    {
        var queryParams = new List<string>();
        
        if (courseId.HasValue)
            queryParams.Add($"courseId={courseId.Value}");
        if (holesPlayed.HasValue)
            queryParams.Add($"holesPlayed={holesPlayed.Value}");
        if (roundType.HasValue)
            queryParams.Add($"roundType={roundType.Value}");
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            
        return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
    }

    public async Task<PlayerReportViewModel?> GetPlayerReportAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        HttpResponseMessage? response = null;
        try
        {
            EnsureAuthorizationHeader();
            
            var queryString = BuildQueryString(courseId, holesPlayed, roundType, startDate, endDate);
            var url = $"api/players/{playerId}/report{queryString}";
            
            _logger.LogInformation("Fetching player report from API: {Url}", url);
            response = await _httpClient.GetAsync(url);
            
            _logger.LogInformation("Player report API response status: {StatusCode}", response.StatusCode);
            
            // Store response details for debugging (this is a hack but we need visibility)
            LastApiError = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Player {PlayerId} not found (404)", playerId);
                LastApiError += " - Player not found";
                return null;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access (401) for player report {PlayerId}", playerId);
                LastApiError += " - Unauthorized";
                return null;
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                LastApiError += $" - {errorContent}";
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Player report API response length: {Length}", json.Length);
            
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("Empty response from player report API");
                LastApiError += " - Empty response";
                return null;
            }
            
            var report = JsonSerializer.Deserialize<PlayerReportViewModel>(json, _jsonOptions);
            LastApiError = $"Success - got {report?.PerformanceData?.Count ?? 0} data points";
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player report for player {PlayerId} from API", playerId);
            LastApiError = $"Exception: {ex.GetType().Name} - {ex.Message}";
            if (response != null)
            {
                LastApiError += $" (HTTP {(int)response.StatusCode})";
            }
            return null;
        }
    }
    
    // Temporary property to expose API error details
    public string LastApiError { get; private set; } = "";

    public async Task<ScoringDistribution?> GetScoringDistributionAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var queryString = BuildQueryString(courseId, holesPlayed, roundType, startDate, endDate);
            var url = $"api/players/{playerId}/scoring-distribution{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var distribution = JsonSerializer.Deserialize<ScoringDistribution>(json, _jsonOptions);
            
            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scoring distribution for player {PlayerId} from API", playerId);
            return null;
        }
    }

    public async Task<PerformanceByPar?> GetPerformanceByParAsync(int playerId, int? courseId = null, int? holesPlayed = null, RoundTypeOption? roundType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var queryString = BuildQueryString(courseId, holesPlayed, roundType, startDate, endDate);
            var url = $"api/players/{playerId}/performance-by-par{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var performance = JsonSerializer.Deserialize<PerformanceByPar>(json, _jsonOptions);
            
            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance by par for player {PlayerId} from API", playerId);
            return null;
        }
    }
}