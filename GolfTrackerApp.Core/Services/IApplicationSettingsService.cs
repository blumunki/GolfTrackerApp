using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IApplicationSettingsService
{
    Task<List<ApplicationSetting>> GetAllAsync();
    Task<ApplicationSetting?> GetByKeyAsync(string key);
    Task<string> GetValueAsync(string key, string defaultValue = "");
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task SaveAsync(string key, string value, string userId);
    Task SeedDefaultsAsync();
}
