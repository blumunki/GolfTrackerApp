using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Authentication;
using System.Text;
using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? JwtToken { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public class GoogleAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ConfigurationService _configurationService;
        private readonly ILogger<GoogleAuthenticationService> _logger;
        private readonly AuthenticationStateService _authStateService;

        private string? _googleClientId;
        private string? _googleClientSecret;

        public GoogleAuthenticationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ConfigurationService configurationService,
            ILogger<GoogleAuthenticationService> logger,
            AuthenticationStateService authStateService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _configurationService = configurationService;
            _logger = logger;
            _authStateService = authStateService;

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
#if DEBUG
                _googleClientId = GolfTrackerApp.Mobile.Generated.DevConfiguration.GoogleClientId;
                _googleClientSecret = GolfTrackerApp.Mobile.Generated.DevConfiguration.GoogleClientSecret;
                
                _logger.LogInformation("Loading Google OAuth configuration from DevConfiguration");
                _logger.LogInformation($"Google Client ID loaded: {!string.IsNullOrEmpty(_googleClientId)}");
#else
                _googleClientId = _configuration["Authentication:Google:ClientId"];
                _googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
#endif

                if (string.IsNullOrEmpty(_googleClientId))
                {
                    _logger.LogError("Google Client ID not configured");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Google OAuth configuration");
            }
        }

        public async Task<AuthenticationResult> GoogleSignInAsync()
        {
            try
            {
                _logger.LogInformation("Starting Google authentication flow");

                if (string.IsNullOrEmpty(_googleClientId))
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Configuration error: Google Client ID not configured"
                    };
                }

                // Use different callback URLs for different platforms
                string callbackUrl;
                string authUrl;

#if IOS
                // iOS uses reverse client ID for native app authentication
                var clientIdParts = _googleClientId.Split('-');
                if (clientIdParts.Length < 2)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid Google Client ID format for iOS"
                    };
                }
                var reverseClientId = $"com.googleusercontent.apps.{clientIdParts[0]}";
                callbackUrl = $"{reverseClientId}://oauth";
#else
                // Android and other platforms use localhost callback
                callbackUrl = "http://localhost:7777/oauth/callback";
#endif

                authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                    $"?client_id={Uri.EscapeDataString(_googleClientId)}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString("openid profile email")}" +
                    $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                    $"&state={Guid.NewGuid()}";

                _logger.LogInformation("Starting WebAuthenticator with callback: {CallbackUrl}", callbackUrl);

                var authResult = await WebAuthenticator.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = new Uri(authUrl),
                        CallbackUrl = new Uri(callbackUrl)
                    });

                if (authResult?.Properties?.ContainsKey("code") == true)
                {
                    var authCode = authResult.Properties["code"];
                    var tokenResult = await ExchangeCodeForTokens(authCode, callbackUrl);
                    
                    if (tokenResult.IsSuccess && !string.IsNullOrEmpty(tokenResult.AccessToken))
                    {
                        // Get user profile from Google
                        var profileResult = await GetUserProfile(tokenResult.AccessToken);
                        if (profileResult.IsSuccess)
                        {
                            tokenResult.Email = profileResult.Email;
                            tokenResult.Name = profileResult.Name;

                            // Send to our API to get JWT token
                            var apiResult = await SendToApi(profileResult.Email!, profileResult.Name!, profileResult.IdToken!, tokenResult.AccessToken);
                            if (apiResult.IsSuccess)
                            {
                                tokenResult.JwtToken = apiResult.JwtToken;
                                tokenResult.UserId = apiResult.UserId;

                                // Set full authentication state
                                _authStateService.SetAuthenticationState(
                                    apiResult.JwtToken!, 
                                    apiResult.UserId!, 
                                    profileResult.Email!, 
                                    profileResult.Name!);

                                // Save to secure storage
                                await _authStateService.SaveTokenSecurelyAsync();
                            }
                        }
                    }

                    return tokenResult;
                }
                else
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No authorization code received"
                    };
                }
            }
            catch (TaskCanceledException)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Authentication was cancelled"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google authentication failed");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Authentication failed: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResult> ExchangeCodeForTokens(string authorizationCode, string redirectUri)
        {
            try
            {
                var tokenRequest = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["client_id"] = _googleClientId!,
                    ["client_secret"] = _googleClientSecret!,
                    ["redirect_uri"] = redirectUri,
                    ["code"] = authorizationCode
                };

                var requestContent = new FormUrlEncodedContent(tokenRequest);
                var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var accessToken = tokenResponse.GetProperty("access_token").GetString();

                    return new AuthenticationResult
                    {
                        IsSuccess = true,
                        AccessToken = accessToken
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Token exchange failed: {response.StatusCode} - {errorContent}");
                    
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Token exchange failed: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token exchange");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Token exchange error: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResult> GetUserProfile(string accessToken)
        {
            try
            {
                using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
                profileRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var profileResponse = await _httpClient.SendAsync(profileRequest);
                if (!profileResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get user profile");
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Failed to get user profile"
                    };
                }

                var profileJson = await profileResponse.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<JsonElement>(profileJson);
                
                var email = profile.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "";
                var name = profile.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
                var googleId = profile.TryGetProperty("id", out var idProp) ? idProp.GetString() : "";
                
                _logger.LogInformation($"User profile received: {email}");

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Email = email,
                    Name = name,
                    IdToken = googleId // Store Google ID in IdToken field for API call
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Profile error: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResult> SendToApi(string email, string name, string googleId, string accessToken)
        {
            try
            {
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

                        _logger.LogInformation("API authentication successful");
                        
                        return new AuthenticationResult
                        {
                            IsSuccess = true,
                            JwtToken = jwtToken,
                            UserId = userId
                        };
                    }
                }
                else
                {
                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"API authentication failed: {errorContent}");
                }

                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "API authentication failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending to API");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"API error: {ex.Message}"
                };
            }
        }

        public void SignOut()
        {
            _authStateService.ClearAuthenticationState();
        }
    }
}
