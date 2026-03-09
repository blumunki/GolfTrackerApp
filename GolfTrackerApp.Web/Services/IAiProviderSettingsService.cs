using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiProviderSettingsService
    {
        Task<List<AiProviderSettings>> GetAllAsync();
        Task<AiProviderSettings?> GetByNameAsync(string providerName);
        Task UpdateAsync(AiProviderSettings settings, string updatedByUserId);
        Task SeedFromConfigAsync();
    }
}
