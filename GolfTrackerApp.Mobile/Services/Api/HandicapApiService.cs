using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

// Mirrors GolfTrackerApp.Core/Models/Api/HandicapDtos.cs — camelCase JSON, Source as string.
public class HandicapRecordDto
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public decimal HandicapIndex { get; set; }
    public string Source { get; set; } = string.Empty;
    public int? GolfClubId { get; set; }
    public string? GolfClubName { get; set; }
    public int? GolfSocietyId { get; set; }
    public string? GolfSocietyName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsManualEntry { get; set; }
}

public class ScoringDifferentialDto
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string? CourseName { get; set; }
    public string? TeeName { get; set; }
    public int AdjustedGrossScore { get; set; }
    public decimal CourseRating { get; set; }
    public int SlopeRating { get; set; }
    public decimal Differential { get; set; }
    public bool IsUsedInCalculation { get; set; }
}

public interface IHandicapApiService
{
    Task<List<HandicapRecordDto>> GetActiveHandicapsAsync(int playerId);
    Task<List<HandicapRecordDto>> GetPersonalHistoryAsync(int playerId);
    Task<List<ScoringDifferentialDto>> GetDifferentialsAsync(int playerId);
}

public class HandicapApiService : IHandicapApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HandicapApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public HandicapApiService(
        HttpClient httpClient,
        ILogger<HandicapApiService> logger,
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

    public Task<List<HandicapRecordDto>> GetActiveHandicapsAsync(int playerId) =>
        GetListAsync<HandicapRecordDto>($"api/handicaps/players/{playerId}/active");

    public Task<List<HandicapRecordDto>> GetPersonalHistoryAsync(int playerId) =>
        GetListAsync<HandicapRecordDto>($"api/handicaps/players/{playerId}/records?source=Personal");

    public Task<List<ScoringDifferentialDto>> GetDifferentialsAsync(int playerId) =>
        GetListAsync<ScoringDifferentialDto>($"api/handicaps/players/{playerId}/differentials");

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Url}", url);
            return new();
        }
    }
}
