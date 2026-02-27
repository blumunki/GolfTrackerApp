using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GolfTrackerApp.Mobile.Services.Api;

// ── DTOs ──

public class ConnectionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("connectedUserId")]
    public string ConnectedUserId { get; set; } = string.Empty;

    [JsonPropertyName("connectedUserName")]
    public string ConnectedUserName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("connectedSince")]
    public DateTime? ConnectedSince { get; set; }

    [JsonPropertyName("requestedAt")]
    public DateTime? RequestedAt { get; set; }
}

public class UserSearchResultDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("existingConnectionStatus")]
    public string? ExistingConnectionStatus { get; set; }

    [JsonPropertyName("isRequestSentByCurrentUser")]
    public bool IsRequestSentByCurrentUser { get; set; }
}

public class MergeResponseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("requestingUserName")]
    public string? RequestingUserName { get; set; }

    [JsonPropertyName("targetUserName")]
    public string? TargetUserName { get; set; }

    [JsonPropertyName("sourcePlayerName")]
    public string SourcePlayerName { get; set; } = string.Empty;

    [JsonPropertyName("targetPlayerName")]
    public string TargetPlayerName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class PlayerQuickStatsDto
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }

    [JsonPropertyName("roundCount")]
    public int RoundCount { get; set; }

    [JsonPropertyName("bestScore")]
    public int? BestScore { get; set; }

    [JsonPropertyName("averageScore")]
    public double? AverageScore { get; set; }

    [JsonPropertyName("lastPlayed")]
    public DateTime? LastPlayed { get; set; }

    [JsonPropertyName("averageVsPar")]
    public double? AverageVsPar { get; set; }
}

// ── Interface ──

public interface IConnectionApiService
{
    // My profile
    Task<Player?> GetMyProfileAsync();
    Task<PlayerQuickStatsDto?> GetPlayerQuickStatsAsync(int playerId);

    // Connections
    Task<List<ConnectionDto>> GetConnectionsAsync();
    Task<List<ConnectionDto>> GetPendingRequestsReceivedAsync();
    Task<List<ConnectionDto>> GetPendingRequestsSentAsync();
    Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm);
    Task<bool> SendConnectionRequestAsync(string targetUserId);
    Task<bool> AcceptConnectionAsync(int connectionId);
    Task<bool> DeclineConnectionAsync(int connectionId);
    Task<bool> RemoveConnectionAsync(int connectionId);

    // Merge
    Task<bool> RequestMergeAsync(int sourcePlayerId, string targetUserId, string? message);
    Task<List<MergeResponseDto>> GetPendingMergeRequestsReceivedAsync();
    Task<List<MergeResponseDto>> GetPendingMergeRequestsSentAsync();
    Task<bool> AcceptMergeAsync(int mergeId);
    Task<bool> DeclineMergeAsync(int mergeId);
    Task<List<Player>> GetMergeablePlayersAsync(string targetUserId);
}

// ── Implementation ──

public class ConnectionApiService : IConnectionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConnectionApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConnectionApiService(
        HttpClient httpClient,
        ILogger<ConnectionApiService> logger,
        AuthenticationStateService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    private void EnsureAuth()
    {
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    // ── My Profile ──

    public async Task<Player?> GetMyProfileAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/me");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Player>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my profile");
            return null;
        }
    }

    public async Task<PlayerQuickStatsDto?> GetPlayerQuickStatsAsync(int playerId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync($"api/players/{playerId}/quick-stats");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PlayerQuickStatsDto>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick stats for player {PlayerId}", playerId);
            return null;
        }
    }

    // ── Connections ──

    public async Task<List<ConnectionDto>> GetConnectionsAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/connections");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ConnectionDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connections");
            return new();
        }
    }

    public async Task<List<ConnectionDto>> GetPendingRequestsReceivedAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/connections/pending-received");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ConnectionDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests received");
            return new();
        }
    }

    public async Task<List<ConnectionDto>> GetPendingRequestsSentAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/connections/pending-sent");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ConnectionDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests sent");
            return new();
        }
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            EnsureAuth();
            var encodedTerm = Uri.EscapeDataString(searchTerm);
            var response = await _httpClient.GetAsync($"api/players/connections/search?searchTerm={encodedTerm}");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UserSearchResultDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return new();
        }
    }

    public async Task<bool> SendConnectionRequestAsync(string targetUserId)
    {
        try
        {
            EnsureAuth();
            var content = new StringContent(
                JsonSerializer.Serialize(new { targetUserId }),
                Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/players/connections/request", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending connection request");
            return false;
        }
    }

    public async Task<bool> AcceptConnectionAsync(int connectionId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.PostAsync($"api/players/connections/{connectionId}/accept", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting connection");
            return false;
        }
    }

    public async Task<bool> DeclineConnectionAsync(int connectionId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.PostAsync($"api/players/connections/{connectionId}/decline", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining connection");
            return false;
        }
    }

    public async Task<bool> RemoveConnectionAsync(int connectionId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.DeleteAsync($"api/players/connections/{connectionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection");
            return false;
        }
    }

    // ── Merge ──

    public async Task<bool> RequestMergeAsync(int sourcePlayerId, string targetUserId, string? message)
    {
        try
        {
            EnsureAuth();
            var content = new StringContent(
                JsonSerializer.Serialize(new { sourcePlayerId, targetUserId, message }),
                Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/players/merge/request", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting merge");
            return false;
        }
    }

    public async Task<List<MergeResponseDto>> GetPendingMergeRequestsReceivedAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/merge/pending-received");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MergeResponseDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending merge requests received");
            return new();
        }
    }

    public async Task<List<MergeResponseDto>> GetPendingMergeRequestsSentAsync()
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync("api/players/merge/pending-sent");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MergeResponseDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sent merge requests");
            return new();
        }
    }

    public async Task<bool> AcceptMergeAsync(int mergeId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.PostAsync($"api/players/merge/{mergeId}/accept", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting merge");
            return false;
        }
    }

    public async Task<bool> DeclineMergeAsync(int mergeId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.PostAsync($"api/players/merge/{mergeId}/decline", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining merge");
            return false;
        }
    }

    public async Task<List<Player>> GetMergeablePlayersAsync(string targetUserId)
    {
        try
        {
            EnsureAuth();
            var response = await _httpClient.GetAsync($"api/players/merge/mergeable/{targetUserId}");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Player>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mergeable players");
            return new();
        }
    }
}
