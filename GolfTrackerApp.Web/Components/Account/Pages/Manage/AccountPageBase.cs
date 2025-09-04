using GolfTrackerApp.Shared.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GolfTrackerApp.Web.Components.Account.Pages.Manage;

public abstract class AccountPageBase : ComponentBase
{
    [CascadingParameter]
    protected Task<AuthenticationState>? AuthState { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    protected UserManager<ApplicationUser> UserManager { get; set; } = default!;

    protected ApplicationUser? CurrentUser { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState!;
        if (!authState.User.Identity?.IsAuthenticated ?? true)
        {
            NavigationManager.NavigateTo("/Account/Login", true);
            return;
        }

        CurrentUser = await UserManager.GetUserAsync(authState.User);
        if (CurrentUser == null)
        {
            NavigationManager.NavigateTo("/Account/Login", true);
            return;
        }

        await OnAuthenticatedInitializedAsync();
    }

    protected virtual Task OnAuthenticatedInitializedAsync() => Task.CompletedTask;
}