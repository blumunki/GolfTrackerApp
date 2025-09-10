using System.Text.Json;

namespace GolfTrackerApp.Mobile.Services;

public class ConfigurationService
{
    private readonly Dictionary<string, string> _configuration = new();

    public ConfigurationService()
    {
        LoadUserSecrets();
    }

    private void LoadUserSecrets()
    {
        try
        {
            // For MAUI iOS, user secrets need to be handled differently
            // Configuration is now loaded directly in MauiProgram.cs for development builds
            // This service is available as fallback but not actively loading secrets
        }
        catch (Exception)
        {
            // Error handling for configuration loading
        }
    }

    public string? GetValue(string key)
    {
        return _configuration.TryGetValue(key, out var value) ? value : null;
    }

    public string? GoogleClientId => GetValue("Authentication:Google:ClientId");
    public string? GoogleClientSecret => GetValue("Authentication:Google:ClientSecret");
}
