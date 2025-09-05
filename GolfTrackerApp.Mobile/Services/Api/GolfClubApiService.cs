using GolfTrackerApp.Mobile.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface IGolfClubApiService
{
    Task<List<GolfClub>> GetAllGolfClubsAsync();
    Task<GolfClub?> GetGolfClubByIdAsync(int id);
    Task<List<GolfClub>> SearchGolfClubsAsync(string searchTerm);
}

public class GolfClubApiService : IGolfClubApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GolfClubApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GolfClubApiService(HttpClient httpClient, ILogger<GolfClubApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<GolfClub>> GetAllGolfClubsAsync()
    {
        try
        {
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
}
