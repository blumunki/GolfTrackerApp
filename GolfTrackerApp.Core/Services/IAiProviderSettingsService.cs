using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiProviderSettingsService
    {
        Task<List<AiProviderSettings>> GetAllAsync();
        Task<AiProviderSettings?> GetByNameAsync(string providerName);
        Task UpdateAsync(AiProviderSettings settings, string updatedByUserId);
        Task UpdatePrioritiesAsync(List<(int Id, int Priority)> priorities, string updatedByUserId);
        Task SeedFromConfigAsync();
    }
}
