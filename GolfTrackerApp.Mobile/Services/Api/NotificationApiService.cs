using GolfTrackerApp.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services.Api;

public class NotificationDto
{
    public int Id { get; set; }
    public int Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface INotificationApiService
{
    Task<List<NotificationDto>> GetNotificationsAsync(int take = 20);
    Task<int> GetUnreadCountAsync();
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync();
    Task<bool> DeleteNotificationAsync(int notificationId);
}

public class NotificationApiService : INotificationApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiService> _logger;
    private readonly AuthenticationStateService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationApiService(
        HttpClient httpClient,
        ILogger<NotificationApiService> logger,
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

    public async Task<List<NotificationDto>> GetNotificationsAsync(int take = 20)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/notifications?take={take}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<NotificationDto>>(json, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return new();
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.GetAsync("api/notifications/unread-count");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("count").GetInt32();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unread notification count");
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.PutAsync($"api/notifications/{notificationId}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.PutAsync("api/notifications/read-all", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return false;
        }
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        try
        {
            EnsureAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/notifications/{notificationId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {Id}", notificationId);
            return false;
        }
    }
}
