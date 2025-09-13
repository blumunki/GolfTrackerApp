using GolfTrackerApp.Mobile.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http.Headers;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface IPlayerApiService
{
    Task<List<Player>> GetAllPlayersAsync();
    Task<Player?> GetPlayerByIdAsync(int id);
    Task<List<Player>> SearchPlayersAsync(string searchTerm);
    Task<Player?> CreatePlayerAsync(Player player);
    Task<bool> UpdatePlayerAsync(int id, Player player);
    Task<bool> DeletePlayerAsync(int id);
}

public class PlayerApiService : IPlayerApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlayerApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public PlayerApiService(
        HttpClient httpClient, 
        ILogger<PlayerApiService> logger,
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

    public async Task<List<Player>> GetAllPlayersAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            
            _logger.LogInformation("Fetching all players from API");
            var response = await _httpClient.GetAsync("api/players");
            
            _logger.LogInformation($"Players API response: {response.StatusCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Players API returned 401 Unauthorized");
                return new List<Player>();
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Players API response content length: {json.Length}");
            
            var players = JsonSerializer.Deserialize<List<Player>>(json, _jsonOptions);
            
            _logger.LogInformation($"Deserialized {players?.Count ?? 0} players");
            return players ?? new List<Player>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching players from API");
            return new List<Player>();
        }
    }

    public async Task<Player?> GetPlayerByIdAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.GetAsync($"api/players/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var player = JsonSerializer.Deserialize<Player>(json, _jsonOptions);
            
            return player;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching player {PlayerId} from API", id);
            return null;
        }
    }

    public async Task<List<Player>> SearchPlayersAsync(string searchTerm)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.GetAsync($"api/players/search?searchTerm={Uri.EscapeDataString(searchTerm)}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var players = JsonSerializer.Deserialize<List<Player>>(json, _jsonOptions);
            
            return players ?? new List<Player>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching players with term '{SearchTerm}' from API", searchTerm);
            return new List<Player>();
        }
    }

    public async Task<Player?> CreatePlayerAsync(Player player)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var json = JsonSerializer.Serialize(player, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/players", content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var createdPlayer = JsonSerializer.Deserialize<Player>(responseJson, _jsonOptions);
            
            return createdPlayer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating player via API");
            return null;
        }
    }

    public async Task<bool> UpdatePlayerAsync(int id, Player player)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var json = JsonSerializer.Serialize(player, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/players/{id}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player {PlayerId} via API", id);
            return false;
        }
    }

    public async Task<bool> DeletePlayerAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            
            var response = await _httpClient.DeleteAsync($"api/players/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting player {PlayerId} via API", id);
            return false;
        }
    }
}
