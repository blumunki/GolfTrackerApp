using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services;

public class GoogleAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthenticationService> _logger;
    private readonly AuthenticationStateService _authenticationStateService;

    public GoogleAuthenticationService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<GoogleAuthenticationService> logger,
        AuthenticationStateService authenticationStateService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authenticationStateService = authenticationStateService;
        
        // Debug logging to see if configuration is available
        _logger.LogInformation("GoogleAuthenticationService initialized");
        Console.WriteLine("DEBUG: GoogleAuthenticationService initialized");
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        _logger.LogInformation($"Client ID configured: {!string.IsNullOrEmpty(clientId)}");
        _logger.LogInformation($"Client Secret configured: {!string.IsNullOrEmpty(clientSecret)}");
        Console.WriteLine($"DEBUG: Client ID configured: {!string.IsNullOrEmpty(clientId)}");
        Console.WriteLine($"DEBUG: Client Secret configured: {!string.IsNullOrEmpty(clientSecret)}");
    }

    public async Task<bool> GoogleSignInAsync()
    {
        try
        {
            _logger.LogInformation("Starting Google Sign-In process");
            Console.WriteLine("DEBUG: Starting Google Sign-In process");
            
            // Get Google OAuth configuration
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];

            _logger.LogInformation($"Client ID configured: {!string.IsNullOrEmpty(clientId)}");
            _logger.LogInformation($"Client Secret configured: {!string.IsNullOrEmpty(clientSecret)}");

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("Google Client ID not configured");
                throw new InvalidOperationException("Google Client ID not configured");
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Google Client Secret not configured");
                throw new InvalidOperationException("Google Client Secret not configured");
            }

            _logger.LogInformation($"Using Google Client ID: {clientId?.Substring(0, 20)}...");

            // Create the authorization URL
            var state = Guid.NewGuid().ToString("N");
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            
            // Use localhost redirect URI for mobile apps (required by Google)
            var redirectUri = "http://localhost:7777/oauth/callback";
            
            _logger.LogInformation($"Client ID: {clientId}");
            _logger.LogInformation($"Using mobile redirect: {redirectUri}");
            
            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={clientId}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                "&response_type=code" +
                "&scope=openid%20email%20profile" +
                $"&state={state}" +
                $"&code_challenge={codeChallenge}" +
                "&code_challenge_method=S256";

            // For testing purposes, let's try opening the URL manually and handle the callback differently
            _logger.LogInformation($"OAuth URL: {authUrl}");
            _logger.LogInformation("For iOS Simulator testing, opening Safari manually");

            string? authCode = null;
            try
            {
                Console.WriteLine("DEBUG: Starting OAuth callback server");
                _logger.LogInformation("Starting OAuth callback server");
                
                // Start the callback server to handle the OAuth redirect
                using var callbackServer = new OAuthCallbackServer(7777, "/oauth/callback");
                
                // Start server and get auth code (with timeout)
                var serverTask = callbackServer.StartAndWaitForCallbackAsync(TimeSpan.FromMinutes(5));
                
                Console.WriteLine("DEBUG: About to call WebAuthenticator.AuthenticateAsync");
                _logger.LogInformation("About to call WebAuthenticator.AuthenticateAsync");
                
                // Use WebAuthenticator to open the browser - must be called on main UI thread
                var authTask = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        return await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(
                            new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                            {
                                Url = new Uri(authUrl),
                                CallbackUrl = new Uri(redirectUri),
                                PrefersEphemeralWebBrowserSession = false
                            });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: WebAuthenticator failed (expected): {ex.Message}");
                        return null; // This is expected since user will cancel Safari
                    }
                });

                // Wait for the callback server to get the authorization code
                // The WebAuthenticator will likely fail when user presses Cancel, but that's OK
                Console.WriteLine("DEBUG: Waiting for authorization code from callback server...");
                authCode = await serverTask;
                
                if (!string.IsNullOrEmpty(authCode))
                {
                    Console.WriteLine($"DEBUG: Authorization code received from callback server: {authCode?.Substring(0, 10)}...");
                    _logger.LogInformation("Authorization code received from callback server");
                }
                else
                {
                    Console.WriteLine("DEBUG: No authorization code received from callback server");
                    _logger.LogError("No authorization code received from callback server");
                    
                    // Try to get result from WebAuthenticator as fallback
                    try
                    {
                        var authResult = await authTask;
                        if (authResult?.Properties.TryGetValue("code", out authCode) == true)
                        {
                            Console.WriteLine($"DEBUG: Got authorization code from WebAuthenticator fallback: {authCode?.Substring(0, 10)}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: WebAuthenticator also failed: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(authCode))
                {
                    Console.WriteLine("DEBUG: No authorization code received from either method");
                    _logger.LogError("No authorization code received");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: WebAuthenticator failed with exception: {ex.GetType().Name}: {ex.Message}");
                _logger.LogError($"WebAuthenticator failed: {ex.Message}");
                _logger.LogError($"WebAuthenticator exception type: {ex.GetType().Name}");
                _logger.LogError($"WebAuthenticator stack trace: {ex.StackTrace}");
                
                // Check if it was user cancellation
                if (ex is TaskCanceledException || ex.Message.Contains("canceled") || ex.Message.Contains("cancelled"))
                {
                    Console.WriteLine("DEBUG: User canceled the authentication process");
                    _logger.LogInformation("User canceled the authentication process");
                    return false;
                }
                
                // For other errors, provide debugging information
                _logger.LogInformation("WebAuthenticator failed. Make sure:");
                _logger.LogInformation("1. Google OAuth client has http://localhost:7777/oauth/callback in redirect URIs");
                _logger.LogInformation("2. Complete the OAuth flow without dismissing Safari manually");
                _logger.LogInformation($"3. OAuth URL for manual testing: {authUrl}");
                
                return false;
            }

            // Exchange authorization code for access token
            var tokenData = new List<KeyValuePair<string, string>>
            {
                new("client_id", clientId!),
                new("client_secret", clientSecret!),
                new("code", authCode!),
                new("grant_type", "authorization_code"),
                new("redirect_uri", redirectUri),
                new("code_verifier", codeVerifier)
            };

            var tokenContent = new FormUrlEncodedContent(tokenData);
            
            Console.WriteLine("DEBUG: Exchanging authorization code for access token");
            _logger.LogInformation("Exchanging authorization code for access token");
            
            var tokenResponse = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenContent);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG: Token exchange failed with status {tokenResponse.StatusCode}: {errorContent}");
                _logger.LogError($"Token exchange failed with status {tokenResponse.StatusCode}: {errorContent}");
                return false;
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            
            if (!tokenResult.TryGetProperty("access_token", out var accessTokenElement))
            {
                _logger.LogError("No access token in response");
                return false;
            }

            var accessToken = accessTokenElement.GetString();
            _logger.LogInformation("Access token received, getting user profile");

            // Get user profile from Google
            using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            profileRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var profileResponse = await _httpClient.SendAsync(profileRequest);
            if (!profileResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get user profile");
                return false;
            }

            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<JsonElement>(profileJson);

            var email = profile.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "";
            var name = profile.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
            var googleId = profile.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";

            _logger.LogInformation($"User profile received: {email}");

            // Send the Google auth data to our API
            var googleSignInData = new
            {
                Email = email,
                Name = name,
                GoogleId = googleId,
                AccessToken = accessToken
            };

            var json = JsonSerializer.Serialize(googleSignInData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending Google auth data to API");
            var apiResponse = await _httpClient.PostAsync("api/auth/google-signin", content);

            if (apiResponse.IsSuccessStatusCode)
            {
                var responseJson = await apiResponse.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (authResponse.TryGetProperty("token", out var tokenProp) &&
                    authResponse.TryGetProperty("userId", out var userIdProp))
                {
                    var jwtToken = tokenProp.GetString();
                    var userId = userIdProp.GetString();

                    _logger.LogInformation("Authentication successful, setting auth state");

                    // Set authentication state
                    _authenticationStateService.SetAuthenticationState(
                        jwtToken!, 
                        userId!, 
                        email!, 
                        name!);

                    // Save to secure storage
                    await _authenticationStateService.SaveTokenSecurelyAsync();

                    return true;
                }
            }
            else
            {
                var errorContent = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogError($"API authentication failed: {errorContent}");
            }

            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("User canceled the authentication");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during Google sign-in");
            throw;
        }
    }

    private static string GenerateCodeVerifier()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 128)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
