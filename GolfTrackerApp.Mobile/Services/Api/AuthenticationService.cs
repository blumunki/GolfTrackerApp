using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GolfTrackerApp.Mobile.Services.Api;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IConfiguration _configuration;

    public AuthenticationService(HttpClient httpClient, ILogger<AuthenticationService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var loginRequest = new LoginRequest { Email = email, Password = password };
            var json = JsonSerializer.Serialize(loginRequest, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);
                if (loginResponse != null)
                {
                    return new AuthResult
                    {
                        IsSuccess = true,
                        Token = loginResponse.Token,
                        UserId = loginResponse.UserId,
                        Email = loginResponse.Email,
                        UserName = loginResponse.UserName
                    };
                }
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorResponse?.Message ?? "Login failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during login"
            };
        }

        return new AuthResult { IsSuccess = false, ErrorMessage = "Login failed" };
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var registerRequest = new RegisterRequest { Email = email, Password = password };
            var json = JsonSerializer.Serialize(registerRequest, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);
                if (loginResponse != null)
                {
                    return new AuthResult
                    {
                        IsSuccess = true,
                        Token = loginResponse.Token,
                        UserId = loginResponse.UserId,
                        Email = loginResponse.Email,
                        UserName = loginResponse.UserName
                    };
                }
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorResponse?.Message ?? "Registration failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during registration"
            };
        }

        return new AuthResult { IsSuccess = false, ErrorMessage = "Registration failed" };
    }

    public async Task<AuthResult> GoogleSignInAsync()
    {
        try
        {
            // Configure Google OAuth - for mobile apps, we need to use the reverse client ID
            _logger.LogInformation("Checking configuration for Google Client ID...");
            
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            _logger.LogInformation("Google Client ID loaded from config: {HasValue}", !string.IsNullOrEmpty(googleClientId));
            
            if (string.IsNullOrEmpty(googleClientId))
                throw new InvalidOperationException("Google Client ID not configured");
            _logger.LogInformation("Starting Google Sign-In with Client ID: {ClientId}", googleClientId);
            
            // For desktop applications, we use localhost redirect or the reverse client ID
            // Extract the numeric part and create reverse client ID
            var clientIdParts = googleClientId.Split('-');
            if (clientIdParts.Length < 2)
                throw new InvalidOperationException("Invalid Google Client ID format");
            
            var reverseClientId = $"com.googleusercontent.apps.{clientIdParts[0]}";
            _logger.LogInformation("Using reverse client ID: {ReverseClientId}", reverseClientId);
            
            var authenticationUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth");
            var callbackUrl = new Uri($"{reverseClientId}://oauth");
            
            var authUrl = $"{authenticationUrl}?" +
                $"client_id={googleClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(callbackUrl.ToString())}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString("openid email profile")}";            _logger.LogInformation("Auth URL: {AuthUrl}", authUrl);
            _logger.LogInformation("Callback URL: {CallbackUrl}", callbackUrl);

            var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(
                new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                {
                    Url = new Uri(authUrl),
                    CallbackUrl = callbackUrl
                });

            _logger.LogInformation("WebAuthenticator result received");
            
            if (authResult?.Properties != null)
            {
                _logger.LogInformation("Auth result properties count: {Count}", authResult.Properties.Count);
                foreach (var prop in authResult.Properties)
                {
                    _logger.LogInformation("Property: {Key} = {Value}", prop.Key, prop.Value);
                }
            }
            else
            {
                _logger.LogWarning("Auth result or properties is null");
            }

            if (authResult?.Properties != null && 
                authResult.Properties.TryGetValue("email", out var email) &&
                !string.IsNullOrEmpty(email))
            {
                // Send the Google authentication to our API
                var googleRequest = new GoogleSignInRequest 
                { 
                    Email = email,
                    Name = authResult.Properties.TryGetValue("name", out var name) ? name : email,
                    IdToken = authResult.Properties.TryGetValue("id_token", out var idToken) ? idToken : ""
                };

                _logger.LogInformation("Sending Google auth request for email: {Email}", email);

                var json = JsonSerializer.Serialize(googleRequest, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/google-signin", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("API response content: {Content}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);
                    if (loginResponse != null)
                    {
                        _logger.LogInformation("Google Sign-In successful for user: {Email}", loginResponse.Email);
                        return new AuthResult
                        {
                            IsSuccess = true,
                            Token = loginResponse.Token,
                            UserId = loginResponse.UserId,
                            Email = loginResponse.Email,
                            UserName = loginResponse.UserName
                        };
                    }
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
                    _logger.LogError("Google Sign-In API error: {Error}", errorResponse?.Message ?? "Unknown error");
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorResponse?.Message ?? "Google sign-in failed"
                    };
                }
            }
            
            _logger.LogWarning("Google authentication was cancelled or failed - no email received");
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = "Google authentication was cancelled or failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google sign-in");
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = $"An error occurred during Google sign-in: {ex.Message}"
            };
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GoogleSignInRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
