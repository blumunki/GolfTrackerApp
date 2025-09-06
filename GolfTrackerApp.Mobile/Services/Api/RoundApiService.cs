using GolfTrackerApp.Mobile.Models;
using Microsoft.Extensions.Logging;
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
}

public interface IRoundApiService
{
    Task<List<RoundResponse>> GetAllRoundsAsync();
    Task<List<RoundResponse>> GetRoundsAsync(int page = 1, int pageSize = 10);
    Task<RoundResponse?> GetRoundByIdAsync(int id);
}

public class RoundApiService : IRoundApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoundApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RoundApiService(HttpClient httpClient, ILogger<RoundApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<RoundResponse>> GetAllRoundsAsync()
    {
        try
        {
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
}
