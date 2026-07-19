using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GolfTrackerApp.Mobile.Models;
using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Mobile.Services.Api;

public interface ICompetitionApiService
{
    string? LastError { get; }
    Task<List<CompetitionSummaryDto>> GetCompetitionsAsync(int? clubId = null, int? societyId = null, string? status = null);
    Task<CompetitionDetailDto?> GetCompetitionAsync(int id);
    Task<CompetitionSummaryDto?> CreateCompetitionAsync(CreateCompetitionRequest request);
    Task<CompetitionSummaryDto?> UpdateCompetitionAsync(int id, UpdateCompetitionRequest request);
    Task<CompetitionSummaryDto?> SetStatusAsync(int id, string status);
    Task<bool> DeleteCompetitionAsync(int id);
    Task<CompetitionRegistrationStatusDto?> GetRegistrationStatusAsync(int id);
    Task<bool> RegisterAsync(int id, int? teeSetId = null);
    Task<bool> WithdrawRegistrationAsync(int id);
    Task<bool> AddEntryAsync(int id, int playerId, int? teeSetId = null);
    Task<bool> RemoveEntryAsync(int id, int playerId);
    Task<bool> AssignRoundAsync(int id, int roundId);
    Task<bool> UnassignRoundAsync(int roundId);
}

public class CompetitionApiService : ICompetitionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CompetitionApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public CompetitionApiService(
        HttpClient httpClient,
        ILogger<CompetitionApiService> logger,
        AuthenticationStateService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;
    }

    public string? LastError { get; private set; }

    public async Task<List<CompetitionSummaryDto>> GetCompetitionsAsync(
        int? clubId = null,
        int? societyId = null,
        string? status = null)
    {
        var query = new List<string>();
        if (clubId.HasValue) query.Add($"clubId={clubId.Value}");
        if (societyId.HasValue) query.Add($"societyId={societyId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");

        var path = "api/competitions" + (query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}");
        return await GetAsync<List<CompetitionSummaryDto>>(path) ?? new();
    }

    public Task<CompetitionDetailDto?> GetCompetitionAsync(int id) =>
        GetAsync<CompetitionDetailDto>($"api/competitions/{id}");

    public Task<CompetitionSummaryDto?> CreateCompetitionAsync(CreateCompetitionRequest request) =>
        SendForJsonAsync<CompetitionSummaryDto>(HttpMethod.Post, "api/competitions", request);

    public Task<CompetitionSummaryDto?> UpdateCompetitionAsync(int id, UpdateCompetitionRequest request) =>
        SendForJsonAsync<CompetitionSummaryDto>(HttpMethod.Put, $"api/competitions/{id}", request);

    public Task<CompetitionSummaryDto?> SetStatusAsync(int id, string status) =>
        SendForJsonAsync<CompetitionSummaryDto>(
            HttpMethod.Put,
            $"api/competitions/{id}/status",
            new CompetitionStatusRequest { Status = status });

    public Task<bool> DeleteCompetitionAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/competitions/{id}");

    public Task<CompetitionRegistrationStatusDto?> GetRegistrationStatusAsync(int id) =>
        GetAsync<CompetitionRegistrationStatusDto>($"api/competitions/{id}/registration");

    public async Task<bool> RegisterAsync(int id, int? teeSetId = null) =>
        await SendForJsonAsync<CompetitionRegistrationStatusDto>(
            HttpMethod.Post,
            $"api/competitions/{id}/registration",
            new CompetitionRegistrationRequest { TeeSetId = teeSetId }) is not null;

    public Task<bool> WithdrawRegistrationAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/competitions/{id}/registration");

    public Task<bool> AddEntryAsync(int id, int playerId, int? teeSetId = null) =>
        SendAsync(
            HttpMethod.Post,
            $"api/competitions/{id}/entries",
            new CompetitionEntryRequest { PlayerId = playerId, TeeSetId = teeSetId });

    public Task<bool> RemoveEntryAsync(int id, int playerId) =>
        SendAsync(HttpMethod.Delete, $"api/competitions/{id}/entries/{playerId}");

    public Task<bool> AssignRoundAsync(int id, int roundId) =>
        SendAsync(HttpMethod.Post, $"api/competitions/{id}/rounds/{roundId}");

    public Task<bool> UnassignRoundAsync(int roundId) =>
        SendAsync(HttpMethod.Delete, $"api/competitions/rounds/{roundId}");

    private async Task<T?> GetAsync<T>(string path)
    {
        LastError = null;
        try
        {
            EnsureAuthorizationHeader();
            using var response = await _httpClient.GetAsync(path);
            if (!response.IsSuccessStatusCode)
            {
                await RecordFailureAsync(response);
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            RecordException(ex, "reading", path);
            return default;
        }
    }

    private async Task<T?> SendForJsonAsync<T>(HttpMethod method, string path, object body)
    {
        LastError = null;
        try
        {
            EnsureAuthorizationHeader();
            using var request = CreateRequest(method, path, body);
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await RecordFailureAsync(response);
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            RecordException(ex, "updating", path);
            return default;
        }
    }

    private async Task<bool> SendAsync(HttpMethod method, string path, object? body = null)
    {
        LastError = null;
        try
        {
            EnsureAuthorizationHeader();
            using var request = CreateRequest(method, path, body);
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode) return true;

            await RecordFailureAsync(response);
            return false;
        }
        catch (Exception ex)
        {
            RecordException(ex, "updating", path);
            return false;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        return request;
    }

    private void EnsureAuthorizationHeader()
    {
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authService.Token);
        }
    }

    private async Task RecordFailureAsync(HttpResponseMessage response)
    {
        var responseText = await response.Content.ReadAsStringAsync();
        LastError = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            HttpStatusCode.NotFound => "The competition or round could not be found.",
            HttpStatusCode.Conflict => string.IsNullOrWhiteSpace(responseText)
                ? "That change conflicts with the competition's current state."
                : responseText,
            _ => string.IsNullOrWhiteSpace(responseText)
                ? $"The server returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : responseText,
        };

        _logger.LogWarning(
            "Competition API request failed with {StatusCode}: {Response}",
            response.StatusCode,
            responseText);
    }

    private void RecordException(Exception ex, string operation, string path)
    {
        LastError = "Unable to reach the competition service. Please try again.";
        _logger.LogError(ex, "Error {Operation} competition API path {Path}", operation, path);
    }
}
