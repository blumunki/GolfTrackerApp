using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class ApplicationSettingsService : IApplicationSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ApplicationSettingsService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ApplicationSetting>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ApplicationSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<ApplicationSetting?> GetByKeyAsync(string key)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "")
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value ?? defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;
        return bool.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;
        return int.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task SaveAsync(string key, string value, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedByUserId = userId;
        }
        await context.SaveChangesAsync();
    }

    public async Task SeedDefaultsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.ApplicationSettings.Select(s => s.Key).ToListAsync();

        var defaults = new List<ApplicationSetting>
        {
            new() { Key = "MaintenanceMode", Value = "false", Description = "When enabled, non-admin users see a maintenance page", Category = "General", ValueType = "bool" },
            new() { Key = "RegistrationEnabled", Value = "true", Description = "Allow new user registrations", Category = "General", ValueType = "bool" },
            new() { Key = "SiteName", Value = "Golf Tracker", Description = "Application display name shown in headers", Category = "General", ValueType = "string" },
            new() { Key = "MaxRoundsPerDay", Value = "5", Description = "Maximum rounds a user can record per day", Category = "Limits", ValueType = "int" },
            new() { Key = "AiInsightsGlobalEnabled", Value = "true", Description = "Global toggle for AI insight features", Category = "AI", ValueType = "bool" },
            new() { Key = "AiMaxRequestsPerHour", Value = "20", Description = "Maximum AI requests per user per hour", Category = "AI", ValueType = "int" },
            new() { Key = "LiveRoundAutoSave", Value = "true", Description = "Auto-save live round scores on hole transitions", Category = "Live Round", ValueType = "bool" },
            new() { Key = "AllowAnonymousClubBrowsing", Value = "true", Description = "Allow non-authenticated users to browse golf clubs", Category = "General", ValueType = "bool" },
        };

        var toAdd = defaults.Where(d => !existing.Contains(d.Key)).ToList();
        if (toAdd.Any())
        {
            context.ApplicationSettings.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }
}
