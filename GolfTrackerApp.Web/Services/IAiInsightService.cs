using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiInsightService
    {
        Task<AiInsightResult> GetDashboardInsightsAsync(string userId,
            CancellationToken cancellationToken = default);

        Task<AiInsightResult> GetPlayerReportInsightsAsync(int playerId,
            int? courseId = null, int? holesPlayed = null,
            CancellationToken cancellationToken = default);

        Task<AiInsightResult> GetClubInsightsAsync(string userId, int clubId,
            CancellationToken cancellationToken = default);

        Task<AiInsightResult> GetCourseInsightsAsync(string userId, int courseId,
            CancellationToken cancellationToken = default);

        Task<AiInsightResult> ChatAsync(string userId, string userMessage,
            int? sessionId = null,
            CancellationToken cancellationToken = default);
    }
}
