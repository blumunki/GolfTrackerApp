using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GolfTrackerApp.Web.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ApiAuth")]
public abstract class BaseApiController : ControllerBase
{
    protected string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               throw new UnauthorizedAccessException("User ID not found in token");
    }
    
    protected string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? 
               throw new UnauthorizedAccessException("User email not found in token");
    }
}
