using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services
{
    public class AiChatService : IAiChatService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<AiChatService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<AiChatSession>> GetSessionsAsync(string userId, int limit = 20)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AiChatSessions
                .AsNoTracking()
                .Where(s => s.ApplicationUserId == userId && !s.IsArchived)
                .OrderByDescending(s => s.LastMessageAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<AiChatSession?> GetSessionAsync(int sessionId, string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AiChatSessions
                .AsNoTracking()
                .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(s => s.AiChatSessionId == sessionId
                                       && s.ApplicationUserId == userId);
        }

        public async Task<AiChatSession> CreateSessionAsync(string userId, string firstMessage)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var session = new AiChatSession
            {
                ApplicationUserId = userId,
                Title = firstMessage.Length > 80
                    ? firstMessage[..80] + "..."
                    : firstMessage
            };
            context.AiChatSessions.Add(session);
            await context.SaveChangesAsync();
            return session;
        }

        public async Task AddMessageAsync(int sessionId, string role, string content)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.AiChatSessionMessages.Add(new AiChatSessionMessage
            {
                AiChatSessionId = sessionId,
                Role = role,
                Content = content
            });
            var session = await context.AiChatSessions.FindAsync(sessionId);
            if (session != null) session.LastMessageAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task<List<AiChatSessionMessage>> GetMessagesAsync(int sessionId, int limit = 50)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.AiChatSessionMessages
                .AsNoTracking()
                .Where(m => m.AiChatSessionId == sessionId)
                .OrderBy(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task ArchiveSessionAsync(int sessionId, string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var session = await context.AiChatSessions
                .FirstOrDefaultAsync(s => s.AiChatSessionId == sessionId
                                       && s.ApplicationUserId == userId);
            if (session != null)
            {
                session.IsArchived = true;
                await context.SaveChangesAsync();
            }
        }
    }
}
