using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace GolfTrackerApp.Web.Components.Account;

internal sealed class IdentityRedirectManager
{
    private readonly NavigationManager _navigationManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityRedirectManager(NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
    {
        _navigationManager = navigationManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = _navigationManager.ToBaseRelativePath(uri);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        
        // If we have an active HTTP context and the response hasn't started,
        // prefer a server-side redirect
        if (httpContext is not null && !httpContext.Response.HasStarted)
        {
            // Handle any status message
            var message = httpContext.Request.Cookies[StatusCookieName];
            if (!string.IsNullOrEmpty(message))
            {
                httpContext.Response.Cookies.Delete(StatusCookieName);
            }

            // Perform server-side redirect
            httpContext.Response.Redirect(_navigationManager.ToAbsoluteUri(uri).ToString());
            return;
        }

        // Fall back to client-side navigation if we can't do server-side redirect
        try
        {
            _navigationManager.NavigateTo(uri, forceLoad: true);
        }
        catch (NavigationException)
        {
            // If navigation fails and we haven't already tried server-side redirect,
            // attempt it now as a last resort
            if (httpContext is not null && !httpContext.Response.HasStarted)
            {
                httpContext.Response.Redirect(uri);
            }
            throw;
        }
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = _navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = _navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    private string CurrentPath => _navigationManager.ToAbsoluteUri(_navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
        => RedirectToWithStatus(CurrentPath, message, context);
}
