using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Mobile.Services;

public class AuthenticationStateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationStateService>? _logger;
    private string? _token;
    private string? _userId;
    private string? _email;
    private string? _userName;
    private int? _playerId;

    public AuthenticationStateService(HttpClient httpClient, ILogger<AuthenticationStateService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public string? UserId => _userId;
    public string? Email => _email;
    public string? UserName => _userName;
    public string? Token => _token;
    public int? PlayerId => _playerId;

    public event Action? AuthenticationStateChanged;

    public void SetAuthenticationState(string token, string userId, string email, string userName, int? playerId = null)
    {
        _token = token;
        _userId = userId;
        _email = email;
        _userName = userName;
        _playerId = playerId;

        // Set the Authorization header for all HTTP requests
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        AuthenticationStateChanged?.Invoke();
    }

    public void ClearAuthenticationState()
    {
        _token = null;
        _userId = null;
        _email = null;
        _userName = null;
        _playerId = null;

        // Remove the Authorization header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        AuthenticationStateChanged?.Invoke();
    }

    public Task SaveTokenSecurelyAsync()
    {
        Console.WriteLine($"[AUTH] SaveTokenSecurelyAsync called");
        Console.WriteLine($"[AUTH] _token: {(_token == null ? "NULL" : $"'{_token}' (length: {_token.Length})")}");
        Console.WriteLine($"[AUTH] _userId: {(_userId == null ? "NULL" : $"'{_userId}'")}");
        Console.WriteLine($"[AUTH] _email: {(_email == null ? "NULL" : $"'{_email}'")}");
        Console.WriteLine($"[AUTH] _userName: {(_userName == null ? "NULL" : $"'{_userName}'")}");
        Console.WriteLine($"[AUTH] _playerId: {(_playerId == null ? "NULL" : $"{_playerId}")}");
        
        if (string.IsNullOrEmpty(_token))
        {
            Console.WriteLine("[AUTH] ERROR: No token to save - _token is null or empty!");
            return Task.CompletedTask;
        }

        try
        {
            // For iOS simulator, just use Preferences directly since SecureStorage has issues
            Preferences.Default.Set("auth_token", _token);
            Preferences.Default.Set("user_id", _userId ?? "");
            Preferences.Default.Set("email", _email ?? "");
            Preferences.Default.Set("username", _userName ?? "");
            if (_playerId.HasValue)
            {
                Preferences.Default.Set("player_id", _playerId.Value);
            }
            Console.WriteLine("[AUTH] Token saved to Preferences successfully");
            _logger?.LogInformation("Token saved to Preferences successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] ERROR saving token: {ex.Message}");
            _logger?.LogError(ex, "Error saving token");
        }
        
        return Task.CompletedTask;
    }

    public async Task LoadTokenFromSecureStorageAsync()
    {
        try
        {
            Console.WriteLine("[AUTH] Loading authentication token from storage");
            _logger?.LogInformation("Loading authentication token from storage");
            
            // Use Preferences directly for simplicity
            var storedToken = Preferences.Default.Get("auth_token", "");
            var storedUserId = Preferences.Default.Get("user_id", "");
            var storedEmail = Preferences.Default.Get("email", "");
            var storedUserName = Preferences.Default.Get("username", "");
            var storedPlayerId = Preferences.Default.Get("player_id", -1);
            
            Console.WriteLine($"[AUTH] Loaded from Preferences:");
            Console.WriteLine($"[AUTH]   auth_token: {(string.IsNullOrEmpty(storedToken) ? "NULL/EMPTY" : $"EXISTS ({storedToken.Length} chars)")}");
            Console.WriteLine($"[AUTH]   user_id: {(string.IsNullOrEmpty(storedUserId) ? "NULL/EMPTY" : storedUserId)}");
            Console.WriteLine($"[AUTH]   email: {(string.IsNullOrEmpty(storedEmail) ? "NULL/EMPTY" : storedEmail)}");
            Console.WriteLine($"[AUTH]   username: {(string.IsNullOrEmpty(storedUserName) ? "NULL/EMPTY" : storedUserName)}");
            Console.WriteLine($"[AUTH]   player_id: {(storedPlayerId == -1 ? "NULL/EMPTY" : storedPlayerId.ToString())}");
            
            _logger?.LogInformation($"Loaded from Preferences:");
            _logger?.LogInformation($"  auth_token: {(string.IsNullOrEmpty(storedToken) ? "NULL/EMPTY" : $"EXISTS ({storedToken.Length} chars)")}");
            _logger?.LogInformation($"  user_id: {(string.IsNullOrEmpty(storedUserId) ? "NULL/EMPTY" : storedUserId)}");
            _logger?.LogInformation($"  email: {(string.IsNullOrEmpty(storedEmail) ? "NULL/EMPTY" : storedEmail)}");
            _logger?.LogInformation($"  username: {(string.IsNullOrEmpty(storedUserName) ? "NULL/EMPTY" : storedUserName)}");
            _logger?.LogInformation($"  player_id: {(storedPlayerId == -1 ? "NULL/EMPTY" : storedPlayerId.ToString())}");

            if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedUserId))
            {
                Console.WriteLine($"[AUTH] Found valid stored credentials for user: {storedEmail}");
                _logger?.LogInformation($"Found valid stored credentials for user: {storedEmail}");
                var playerId = storedPlayerId != -1 ? storedPlayerId : (int?)null;
                SetAuthenticationState(storedToken, storedUserId, storedEmail ?? "", storedUserName ?? "", playerId);
                Console.WriteLine("[AUTH] Authentication state restored from storage");
                _logger?.LogInformation("Authentication state restored from storage");
            }
            else
            {
                Console.WriteLine("[AUTH] No valid stored authentication credentials found");
                _logger?.LogInformation("No valid stored authentication credentials found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] Error loading token from storage: {ex.Message}");
            _logger?.LogError(ex, "Error loading token from storage");
            await ClearStoredTokenAsync();
        }
    }

    private async Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            // Try a simple API call to validate the token
            var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync("api/auth/validate");
            
            // Restore original auth header
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task ClearStoredTokenAsync()
    {
        _logger?.LogInformation("Clearing stored authentication token");
        Console.WriteLine("[AUTH] Clearing stored authentication token");
        
        // Clear from Preferences
        Preferences.Default.Remove("auth_token");
        Preferences.Default.Remove("user_id");
        Preferences.Default.Remove("email");
        Preferences.Default.Remove("username");
        Preferences.Default.Remove("player_id");
        
        ClearAuthenticationState();
        return Task.CompletedTask;
    }

    public async Task LogoutAsync()
    {
        _logger?.LogInformation("User logging out");
        await ClearStoredTokenAsync();
        // Additional logout logic could go here (e.g., calling logout API)
    }
}
