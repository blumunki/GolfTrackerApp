using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiAuditService
    {
        Task LogAsync(AiAuditLog entry);
        Task<bool> IsRateLimitedAsync(string userId);
        Task<int> GetUsageCountAsync(string userId, TimeSpan window);
        Task<int> GetTotalTokensUsedAsync(string userId, TimeSpan window);
    }
}
