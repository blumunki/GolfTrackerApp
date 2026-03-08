using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiChatService
    {
        Task<List<AiChatSession>> GetSessionsAsync(string userId, int limit = 20);
        Task<AiChatSession?> GetSessionAsync(int sessionId, string userId);
        Task<AiChatSession> CreateSessionAsync(string userId, string firstMessage);
        Task AddMessageAsync(int sessionId, string role, string content);
        Task<List<AiChatSessionMessage>> GetMessagesAsync(int sessionId, int limit = 50);
        Task ArchiveSessionAsync(int sessionId, string userId);
    }
}
