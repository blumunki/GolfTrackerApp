using System.Net.Http.Headers;

namespace GolfTrackerApp.Mobile.Services;

public class AuthenticationStateService
{
    private readonly HttpClient _httpClient;
    private string? _token;
    private string? _userId;
    private string? _email;
    private string? _userName;

    public AuthenticationStateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public string? UserId => _userId;
    public string? Email => _email;
    public string? UserName => _userName;

    public event Action? AuthenticationStateChanged;

    public void SetAuthenticationState(string token, string userId, string email, string userName)
    {
        _token = token;
        _userId = userId;
        _email = email;
        _userName = userName;

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

        // Remove the Authorization header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        AuthenticationStateChanged?.Invoke();
    }

    // For future use with secure storage
    public async Task SaveTokenSecurelyAsync()
    {
        // TODO: Implement secure token storage using Microsoft.Maui.Authentication.WebAuthenticator
        // or SecureStorage for production
        if (_token != null)
        {
            await SecureStorage.Default.SetAsync("auth_token", _token);
            await SecureStorage.Default.SetAsync("user_id", _userId ?? "");
            await SecureStorage.Default.SetAsync("email", _email ?? "");
            await SecureStorage.Default.SetAsync("username", _userName ?? "");
        }
    }

    public async Task LoadTokenFromSecureStorageAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            var userId = await SecureStorage.Default.GetAsync("user_id");
            var email = await SecureStorage.Default.GetAsync("email");
            var userName = await SecureStorage.Default.GetAsync("username");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
            {
                SetAuthenticationState(token, userId, email ?? "", userName ?? "");
            }
        }
        catch (Exception)
        {
            // Token might be corrupted, clear it
            await ClearStoredTokenAsync();
        }
    }

    public Task ClearStoredTokenAsync()
    {
        SecureStorage.Default.Remove("auth_token");
        SecureStorage.Default.Remove("user_id");
        SecureStorage.Default.Remove("email");
        SecureStorage.Default.Remove("username");
        ClearAuthenticationState();
        return Task.CompletedTask;
    }
}
