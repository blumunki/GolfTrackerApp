using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface ISocietyApiService
{
    Task<List<GolfSocietyDto>> GetAllSocietiesAsync();
    Task<List<GolfSocietyDto>> GetMySocietiesAsync();
    Task<SocietyDetailDto?> GetSocietyByIdAsync(int id);
    Task<GolfSocietyDto?> CreateSocietyAsync(string name, string? description);
    Task<bool> JoinSocietyAsync(int societyId);
    Task<bool> LeaveSocietyAsync(int societyId);
    Task<bool> DeleteSocietyAsync(int societyId);
}

public class SocietyApiService : ISocietyApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SocietyApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public SocietyApiService(HttpClient httpClient, ILogger<SocietyApiService> logger, AuthenticationStateService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    private void EnsureAuthorizationHeader()
    {
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    public async Task<List<GolfSocietyDto>> GetAllSocietiesAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/societies");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GolfSocietyDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching societies");
            return new();
        }
    }

    public async Task<List<GolfSocietyDto>> GetMySocietiesAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/societies/my");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GolfSocietyDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my societies");
            return new();
        }
    }

    public async Task<SocietyDetailDto?> GetSocietyByIdAsync(int id)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/societies/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SocietyDetailDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching society {Id}", id);
            return null;
        }
    }

    public async Task<GolfSocietyDto?> CreateSocietyAsync(string name, string? description)
    {
        try
        {
            EnsureAuthorizationHeader();
            var body = JsonSerializer.Serialize(new { name, description }, _jsonOptions);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/societies", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GolfSocietyDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating society");
            return null;
        }
    }

    public async Task<bool> JoinSocietyAsync(int societyId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/societies/{societyId}/join", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining society {Id}", societyId);
            return false;
        }
    }

    public async Task<bool> LeaveSocietyAsync(int societyId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/societies/{societyId}/leave", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving society {Id}", societyId);
            return false;
        }
    }

    public async Task<bool> DeleteSocietyAsync(int societyId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/societies/{societyId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting society {Id}", societyId);
            return false;
        }
    }
}
