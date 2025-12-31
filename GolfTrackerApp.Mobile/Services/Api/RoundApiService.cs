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

public interface IRoundApiService
{
    Task<List<RoundResponse>> GetAllRoundsAsync();
    Task<List<RoundResponse>> GetRoundsAsync(int page = 1, int pageSize = 10);
    Task<RoundResponse?> GetRoundByIdAsync(int id);
    Task<List<ScoreResponse>> GetRoundScoresAsync(int roundId);
    Task<RoundResponse?> CreateRoundAsync(CreateRoundRequest request);
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
        Debug.WriteLine($"[ROUND_API] EnsureAuthorizationHeader called - IsAuth: {_authService.IsAuthenticated}, HasToken: {!string.IsNullOrEmpty(_authService.Token)}");
        
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            Debug.WriteLine($"[ROUND_API] Setting Authorization header with token (length: {_authService.Token.Length})");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
            Debug.WriteLine($"[ROUND_API] Header set. Current auth header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "NULL"}");
        }
        else
        {
            Debug.WriteLine("[ROUND_API] WARNING: Not setting auth header - not authenticated or no token");
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
            
            Debug.WriteLine($"[ROUND_API] Creating round - Auth: {_authService.IsAuthenticated}, Token exists: {!string.IsNullOrEmpty(_authService.Token)}");
            _logger.LogInformation("[ROUND_API] Creating round - Auth: {IsAuth}, Token: {HasToken}", 
                _authService.IsAuthenticated, !string.IsNullOrEmpty(_authService.Token));
            
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            Debug.WriteLine($"[ROUND_API] Request JSON length: {json.Length}");
            Debug.WriteLine($"[ROUND_API] Request JSON: {json}");
            Debug.WriteLine($"[ROUND_API] Sending POST to api/rounds");
            _logger.LogInformation("[ROUND_API] Sending POST to api/rounds");
            var response = await _httpClient.PostAsync("api/rounds", content);
            
            Debug.WriteLine($"[ROUND_API] Response status: {response.StatusCode}");
            _logger.LogInformation("[ROUND_API] Response status: {Status}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[ROUND_API] Error response: {errorContent}");
            }
            
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var createdRound = JsonSerializer.Deserialize<RoundResponse>(responseJson, _jsonOptions);
            
            return createdRound;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ROUND_API] ERROR: {ex.Message}");
            Debug.WriteLine($"[ROUND_API] ERROR Type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[ROUND_API] INNER ERROR: {ex.InnerException.Message}");
            }
            _logger.LogError(ex, "Error creating round via API");
            throw;
        }
    }
}
