using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface IInsightsApiService
{
    Task<AiInsightResult?> GetDashboardInsightsAsync();
    Task<AiInsightResult?> GetPlayerReportInsightsAsync(int playerId,
        int? courseId = null, int? holesPlayed = null);
    Task<AiInsightResult?> GetClubInsightsAsync(int clubId);
    Task<AiInsightResult?> GetCourseInsightsAsync(int courseId);
    Task<AiInsightResult?> ChatAsync(string message, int? sessionId = null);
}

public class InsightsApiService : IInsightsApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateService _authService;
    private readonly ILogger<InsightsApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InsightsApiService(
        HttpClient httpClient,
        AuthenticationStateService authService,
        ILogger<InsightsApiService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    private void EnsureAuthorizationHeader()
    {
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    public async Task<AiInsightResult?> GetDashboardInsightsAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/insights/dashboard");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> GetPlayerReportInsightsAsync(
        int playerId, int? courseId = null, int? holesPlayed = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            var url = $"api/insights/player-report/{playerId}";
            var queryParams = new List<string>();
            if (courseId.HasValue) queryParams.Add($"courseId={courseId}");
            if (holesPlayed.HasValue) queryParams.Add($"holesPlayed={holesPlayed}");
            if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player report insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> GetClubInsightsAsync(int clubId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/insights/club/{clubId}");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching club insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> GetCourseInsightsAsync(int courseId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/insights/course/{courseId}");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course insights");
            return null;
        }
    }

    public async Task<AiInsightResult?> ChatAsync(string message, int? sessionId = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            var request = new { Message = message, SessionId = sessionId };
            var content = JsonContent.Create(request, options: _jsonOptions);
            var response = await _httpClient.PostAsync("api/insights/chat", content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AiInsightResult>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            return null;
        }
    }
}
