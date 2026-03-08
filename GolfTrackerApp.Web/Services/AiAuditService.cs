using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services
{
    public class AiAuditService : IAiAuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiAuditService> _logger;

        public AiAuditService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IConfiguration configuration,
            ILogger<AiAuditService> logger)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task LogAsync(AiAuditLog entry)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.AiAuditLogs.Add(entry);
            await context.SaveChangesAsync();
        }

        public async Task<bool> IsRateLimitedAsync(string userId)
        {
            var limit = _configuration.GetValue<int>("AiInsights:RateLimitPerUserPerHour");
            if (limit <= 0) return false;
            var count = await GetUsageCountAsync(userId, TimeSpan.FromHours(1));
            return count >= limit;
        }

        public async Task<int> GetUsageCountAsync(string userId, TimeSpan window)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var since = DateTime.UtcNow - window;
            return await context.AiAuditLogs
                .AsNoTracking()
                .CountAsync(a => a.ApplicationUserId == userId
                              && a.RequestedAt >= since
                              && a.Success);
        }

        public async Task<int> GetTotalTokensUsedAsync(string userId, TimeSpan window)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var since = DateTime.UtcNow - window;
            return await context.AiAuditLogs
                .AsNoTracking()
                .Where(a => a.ApplicationUserId == userId
                         && a.RequestedAt >= since
                         && a.Success)
                .SumAsync(a => a.TotalTokens);
        }
    }
}
