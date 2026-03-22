using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface IClubMembershipApiService
{
    Task<List<ClubMembershipDto>> GetMyMembershipsAsync();
    Task<bool> JoinClubAsync(int clubId, string? membershipNumber = null);
    Task<bool> LeaveClubAsync(int clubId);
}

public class ClubMembershipApiService : IClubMembershipApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClubMembershipApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClubMembershipApiService(HttpClient httpClient, ILogger<ClubMembershipApiService> logger, AuthenticationStateService authService)
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

    public async Task<List<ClubMembershipDto>> GetMyMembershipsAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/clubmemberships/my");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ClubMembershipDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching club memberships");
            return new();
        }
    }

    public async Task<bool> JoinClubAsync(int clubId, string? membershipNumber = null)
    {
        try
        {
            EnsureAuthorizationHeader();
            var body = membershipNumber != null
                ? JsonSerializer.Serialize(new { membershipNumber }, _jsonOptions)
                : "{}";
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"api/clubmemberships/{clubId}/join", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining club {Id}", clubId);
            return false;
        }
    }

    public async Task<bool> LeaveClubAsync(int clubId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.PostAsync($"api/clubmemberships/{clubId}/leave", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving club {Id}", clubId);
            return false;
        }
    }
}
