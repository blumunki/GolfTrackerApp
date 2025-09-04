using Microsoft.AspNetCore.Identity;
using GolfTrackerApp.Shared.Data;

namespace GolfTrackerApp.Web.Components.Account;

internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
    {
        if (context?.User == null)
        {
            throw new InvalidOperationException("No user available.");
        }

        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
            throw new InvalidOperationException($"Unable to load user.");
        }

        return user;
    }
}
