using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            _logger.LogInformation($"Google sign-in request received for email: {request.Email}");
            
            // Validate the request
            if (string.IsNullOrEmpty(request.Email))
            {
                _logger.LogWarning("Google sign-in request missing email");
                return BadRequest(new { message = "Email is required" });
            }
            
            if (string.IsNullOrEmpty(request.GoogleId))
            {
                _logger.LogWarning("Google sign-in request missing GoogleId");
                return BadRequest(new { message = "Google ID is required" });
            }
            
            // For mobile app, we'll trust the Google ID token and create/login the user
            // In production, you should verify the ID token with Google
            
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation($"Creating new user for Google sign-in: {request.Email}");
                
                // Create new user
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError($"Failed to create user for Google sign-in: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return BadRequest(new { message = "Failed to create user account", errors = result.Errors });
                }
                
                _logger.LogInformation($"Successfully created new user: {user.Id}");
            }
            else
            {
                _logger.LogInformation($"Existing user found for Google sign-in: {user.Id}");
            }

            var token = await GenerateJwtToken(user);
            
            _logger.LogInformation($"Successfully generated JWT token for user: {user.Id}");
            
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
        var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-that-is-at-least-32-characters-long";
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
    public string GoogleId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
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
