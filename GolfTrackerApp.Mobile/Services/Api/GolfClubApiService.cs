using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface IGolfClubApiService
{
    Task<List<GolfClub>> GetAllGolfClubsAsync();
    Task<GolfClub?> GetGolfClubByIdAsync(int id);
    Task<List<GolfClub>> SearchGolfClubsAsync(string searchTerm);
    Task<GolfClub?> CreateGolfClubAsync(GolfClub club);
    Task<GolfClub?> UpdateGolfClubAsync(GolfClub club);
    Task<bool> DeleteGolfClubAsync(int id);
}

public class GolfClubApiService : IGolfClubApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GolfClubApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public GolfClubApiService(
        HttpClient httpClient, 
        ILogger<GolfClubApiService> logger,
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

    public async Task<List<GolfClub>> GetAllGolfClubsAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/golfclubs");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var clubs = JsonSerializer.Deserialize<List<GolfClub>>(json, _jsonOptions);
            
            return clubs ?? new List<GolfClub>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching golf clubs from API");
            return new List<GolfClub>();
        }
    }

    public async Task<GolfClub?> GetGolfClubByIdAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/golfclubs/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var club = JsonSerializer.Deserialize<GolfClub>(json, _jsonOptions);
            
            return club;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching golf club {ClubId} from API", id);
            return null;
        }
    }

    public async Task<List<GolfClub>> SearchGolfClubsAsync(string searchTerm)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/golfclubs/search?searchTerm={Uri.EscapeDataString(searchTerm)}");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var clubs = JsonSerializer.Deserialize<List<GolfClub>>(json, _jsonOptions);
            
            return clubs ?? new List<GolfClub>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching golf clubs with term '{SearchTerm}' from API", searchTerm);
            return new List<GolfClub>();
        }
    }

    public async Task<GolfClub?> CreateGolfClubAsync(GolfClub club)
    {
        try
        {
            EnsureAuthorizationHeader();
            var json = JsonSerializer.Serialize(club, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/golfclubs", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GolfClub>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating golf club");
            return null;
        }
    }

    public async Task<GolfClub?> UpdateGolfClubAsync(GolfClub club)
    {
        try
        {
            EnsureAuthorizationHeader();
            var json = JsonSerializer.Serialize(club, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/golfclubs/{club.GolfClubId}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GolfClub>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating golf club {ClubId}", club.GolfClubId);
            return null;
        }
    }

    public async Task<bool> DeleteGolfClubAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/golfclubs/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting golf club {ClubId}", id);
            return false;
        }
    }
}
