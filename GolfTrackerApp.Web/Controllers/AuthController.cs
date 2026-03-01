using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models.Api;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            var token = await GenerateJwtToken(user);
            
            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already registered" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true // For mobile app, skip email confirmation
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Registration failed", errors = result.Errors });
            }

            var token = await GenerateJwtToken(user);
            
            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("google-signin")]
    public async Task<ActionResult<LoginResponse>> GoogleSignIn([FromBody] GoogleSignInRequest request)
    {
        try
        {
            _logger.LogInformation("Google sign-in request received (IdToken length: {Length})", request.IdToken?.Length ?? 0);
            
            if (string.IsNullOrEmpty(request.IdToken))
            {
                _logger.LogWarning("Google sign-in request missing IdToken");
                return BadRequest(new { message = "Google ID token is required" });
            }
            
            // Verify the Google ID token
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var googleClientId = _configuration["Authentication:Google:ClientId"];
                var mobileClientId = _configuration["Authentication:Google:MobileClientId"];
                var audiences = new List<string>();
                if (!string.IsNullOrEmpty(googleClientId)) audiences.Add(googleClientId);
                if (!string.IsNullOrEmpty(mobileClientId)) audiences.Add(mobileClientId);
                _logger.LogInformation("Validating Google ID token (audiences: {Audiences})", string.Join(", ", audiences));
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = audiences.Count > 0 ? audiences : null
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
                _logger.LogInformation("Google ID token validated successfully for {Email}", payload.Email);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token");
                return Unauthorized(new { message = $"Invalid Google ID token: {ex.Message}" });
            }
            catch (Exception ex) when (ex is not InvalidJwtException)
            {
                _logger.LogError(ex, "Error validating Google ID token");
                return StatusCode(500, new { message = $"Token validation error: {ex.Message}" });
            }
            
            var email = payload.Email;
            var googleId = payload.Subject;
            
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email not available in Google token" });
            }
            
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation("Creating new user for Google sign-in: {Email}", email);
                
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create user for Google sign-in: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to create user account", errors = result.Errors });
                }
                
                _logger.LogInformation("Successfully created new user: {UserId}", user.Id);
            }
            else
            {
                _logger.LogInformation("Existing user found for Google sign-in: {UserId}", user.Id);
            }

            var token = await GenerateJwtToken(user);
            
            return Ok(new LoginResponse
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                UserName = user.UserName!
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google sign-in");
            return StatusCode(500, new { message = "An error occurred during Google sign-in" });
        }
    }

    private Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("JWT signing key must be configured via 'Jwt:Key'.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "GolfTrackerApp";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "GolfTrackerApp";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }
}

