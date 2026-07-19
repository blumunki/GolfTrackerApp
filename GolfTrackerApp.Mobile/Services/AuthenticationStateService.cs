using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
    private HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

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
    public bool IsInRole(string role) => _roles.Contains(role);

    public event Action? AuthenticationStateChanged;

    public void SetAuthenticationState(string token, string userId, string email, string userName, int? playerId = null)
    {
        _token = token;
        _userId = userId;
        _email = email;
        _userName = userName;
        _playerId = playerId;
        _roles = ReadRoles(token);

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
        _roles.Clear();

        // Remove the Authorization header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        AuthenticationStateChanged?.Invoke();
    }

    public Task SaveTokenSecurelyAsync()
    {
        
        if (string.IsNullOrEmpty(_token))
        {
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
            _logger?.LogInformation("Token saved to Preferences successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving token");
        }
        
        return Task.CompletedTask;
    }

    public async Task LoadTokenFromSecureStorageAsync()
    {
        try
        {
            _logger?.LogInformation("Loading authentication token from storage");
            
            // Use Preferences directly for simplicity
            var storedToken = Preferences.Default.Get("auth_token", "");
            var storedUserId = Preferences.Default.Get("user_id", "");
            var storedEmail = Preferences.Default.Get("email", "");
            var storedUserName = Preferences.Default.Get("username", "");
            var storedPlayerId = Preferences.Default.Get("player_id", -1);
            
            
            _logger?.LogInformation($"Loaded from Preferences:");
            _logger?.LogInformation($"  auth_token: {(string.IsNullOrEmpty(storedToken) ? "NULL/EMPTY" : $"EXISTS ({storedToken.Length} chars)")}");
            _logger?.LogInformation($"  user_id: {(string.IsNullOrEmpty(storedUserId) ? "NULL/EMPTY" : storedUserId)}");
            _logger?.LogInformation($"  email: {(string.IsNullOrEmpty(storedEmail) ? "NULL/EMPTY" : storedEmail)}");
            _logger?.LogInformation($"  username: {(string.IsNullOrEmpty(storedUserName) ? "NULL/EMPTY" : storedUserName)}");
            _logger?.LogInformation($"  player_id: {(storedPlayerId == -1 ? "NULL/EMPTY" : storedPlayerId.ToString())}");

            if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedUserId))
            {
                _logger?.LogInformation($"Found valid stored credentials for user: {storedEmail}");
                var playerId = storedPlayerId != -1 ? storedPlayerId : (int?)null;
                SetAuthenticationState(storedToken, storedUserId, storedEmail ?? "", storedUserName ?? "", playerId);
                _logger?.LogInformation("Authentication state restored from storage");
            }
            else
            {
                _logger?.LogInformation("No valid stored authentication credentials found");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading token from storage");
            await ClearStoredTokenAsync();
        }
    }

    public Task ClearStoredTokenAsync()
    {
        _logger?.LogInformation("Clearing stored authentication token");
        
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

    private static HashSet<string> ReadRoles(string token)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return roles;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name is not (ClaimTypes.Role or "role" or "roles")) continue;

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    AddRole(roles, property.Value.GetString());
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var value in property.Value.EnumerateArray())
                    {
                        if (value.ValueKind == JsonValueKind.String) AddRole(roles, value.GetString());
                    }
                }
            }
        }
        catch (Exception)
        {
            // The token is still validated by the API; roles only control mobile UI hints.
        }

        return roles;
    }

    private static void AddRole(HashSet<string> roles, string? role)
    {
        if (!string.IsNullOrWhiteSpace(role)) roles.Add(role);
    }
}
