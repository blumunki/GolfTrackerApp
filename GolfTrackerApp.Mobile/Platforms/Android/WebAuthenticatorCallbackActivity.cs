using Android.App;
using Android.Content;
using Android.Content.PM;

namespace GolfTrackerApp.Mobile.Platforms.Android
{
    [Activity(
        NoHistory = true,
        LaunchMode = LaunchMode.SingleTop,
        Exported = true)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "http",
        DataHost = "localhost",
        DataPort = "7777",
        DataPath = "/oauth/callback")]
    public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
    }
}