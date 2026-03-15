using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

// Response models to match the Web API format
public class RoundResponse
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int HolesPlayed { get; set; }
    public string? Notes { get; set; }
    public int PlayerCount { get; set; }
    public string RoundType { get; set; } = string.Empty;
    public string? CreatedByApplicationUserId { get; set; }
    public List<string> PlayingPartners { get; set; } = new();
}

public class ScoreResponse
{
    public int ScoreId { get; set; }
    public int RoundId { get; set; }
    public int PlayerId { get; set; }
    public int HoleId { get; set; }
    public int Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
    public PlayerResponse? Player { get; set; }
    public HoleResponse? Hole { get; set; }
}

public class PlayerResponse
{
    public int PlayerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
}

public class HoleResponse
{
    public int HoleId { get; set; }
    public int HoleNumber { get; set; }
    public int Par { get; set; }
}

public class ScoreUpdateRequest
{
    public int ScoreId { get; set; }
    public int Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
}

public interface IRoundApiService
{
    Task<List<RoundResponse>> GetAllRoundsAsync();
    Task<List<RoundResponse>> GetRoundsAsync(int page = 1, int pageSize = 10);
    Task<RoundResponse?> GetRoundByIdAsync(int id);
    Task<List<ScoreResponse>> GetRoundScoresAsync(int roundId);
    Task<RoundResponse?> CreateRoundAsync(CreateRoundRequest request);
    Task<bool> UpdateScoresAsync(int roundId, List<ScoreUpdateRequest> scores);
    Task<bool> DeleteRoundAsync(int id);
}

public class RoundApiService : IRoundApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoundApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoundApiService(
        HttpClient httpClient, 
        ILogger<RoundApiService> logger,
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
        else
        {
        }
    }

    public async Task<List<RoundResponse>> GetAllRoundsAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/rounds");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var rounds = JsonSerializer.Deserialize<List<RoundResponse>>(json, _jsonOptions);
            
            return rounds ?? new List<RoundResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rounds from API");
            return new List<RoundResponse>();
        }
    }

    public async Task<List<RoundResponse>> GetRoundsAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/rounds?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var rounds = JsonSerializer.Deserialize<List<RoundResponse>>(json, _jsonOptions);
            
            return rounds ?? new List<RoundResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rounds from API");
            return new List<RoundResponse>();
        }
    }

    public async Task<RoundResponse?> GetRoundByIdAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/rounds/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var round = JsonSerializer.Deserialize<RoundResponse>(json, _jsonOptions);
            
            return round;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching round {RoundId} from API", id);
            return null;
        }
    }

    public async Task<List<ScoreResponse>> GetRoundScoresAsync(int roundId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/rounds/{roundId}/scores");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var scores = JsonSerializer.Deserialize<List<ScoreResponse>>(json, _jsonOptions);
            
            return scores ?? new List<ScoreResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching scores for round {RoundId} from API", roundId);
            return new List<ScoreResponse>();
        }
    }

    public async Task<RoundResponse?> CreateRoundAsync(CreateRoundRequest request)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            _logger.LogInformation("[ROUND_API] Creating round - Auth: {IsAuth}, Token: {HasToken}", 
                _authService.IsAuthenticated, !string.IsNullOrEmpty(_authService.Token));
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogInformation("[ROUND_API] Sending POST to api/rounds");
            var response = await _httpClient.PostAsync("api/rounds", content);
            
            _logger.LogInformation("[ROUND_API] Response status: {Status}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
            }
            
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var createdRound = JsonSerializer.Deserialize<RoundResponse>(responseJson, _jsonOptions);
            
            return createdRound;
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
            }
            _logger.LogError(ex, "Error creating round via API");
            throw;
        }
    }

    public async Task<bool> UpdateScoresAsync(int roundId, List<ScoreUpdateRequest> scores)
    {
        try
        {
            EnsureAuthorizationHeader();
            var json = JsonSerializer.Serialize(scores, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/rounds/{roundId}/scores", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scores for round {RoundId}", roundId);
            return false;
        }
    }

    public async Task<bool> DeleteRoundAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/rounds/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting round {RoundId}", id);
            return false;
        }
    }
}
