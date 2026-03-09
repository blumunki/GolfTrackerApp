using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services
{
    public class AiProviderSettingsService : IAiProviderSettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiProviderSettingsService> _logger;

        public AiProviderSettingsService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IConfiguration configuration,
            ILogger<AiProviderSettingsService> logger)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<AiProviderSettings>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AiProviderSettings
                .OrderBy(s => s.Priority)
                .ToListAsync();
        }

        public async Task<AiProviderSettings?> GetByNameAsync(string providerName)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AiProviderSettings
                .FirstOrDefaultAsync(s => s.ProviderName == providerName);
        }

        public async Task UpdateAsync(AiProviderSettings settings, string updatedByUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.AiProviderSettings
                .FirstOrDefaultAsync(s => s.AiProviderSettingsId == settings.AiProviderSettingsId);

            if (existing == null)
            {
                _logger.LogWarning("AiProviderSettings {Id} not found for update", settings.AiProviderSettingsId);
                return;
            }

            existing.Enabled = settings.Enabled;
            existing.Priority = settings.Priority;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = updatedByUserId;

            await context.SaveChangesAsync();
            _logger.LogInformation("Provider {Provider} updated: Enabled={Enabled}, Priority={Priority} by {User}",
                existing.ProviderName, existing.Enabled, existing.Priority, updatedByUserId);
        }

        public async Task UpdatePrioritiesAsync(List<(int Id, int Priority)> priorities, string updatedByUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var allSettings = await context.AiProviderSettings.ToListAsync();

            foreach (var (id, priority) in priorities)
            {
                var setting = allSettings.FirstOrDefault(s => s.AiProviderSettingsId == id);
                if (setting != null)
                {
                    setting.Priority = priority;
                    setting.UpdatedAt = DateTime.UtcNow;
                    setting.UpdatedByUserId = updatedByUserId;
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Updated priorities for {Count} providers by {User}", priorities.Count, updatedByUserId);
        }

        public async Task SeedFromConfigAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await context.AiProviderSettings.AnyAsync())
            {
                _logger.LogDebug("AiProviderSettings already seeded, skipping");
                return;
            }

            var providersSection = _configuration.GetSection("AiInsights:Providers");
            foreach (var child in providersSection.GetChildren())
            {
                context.AiProviderSettings.Add(new AiProviderSettings
                {
                    ProviderName = child.Key,
                    Enabled = false, // Off by default — admin enables
                    Priority = child.GetValue<int>("Priority")
                });
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} AI provider settings from configuration",
                providersSection.GetChildren().Count());
        }
    }
}
