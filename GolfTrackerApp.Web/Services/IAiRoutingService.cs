using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiRoutingService
    {
        Task<AiInsightResult> RouteCompletionAsync(
            string systemPrompt,
            string userPrompt,
            int? maxTokens = null,
            double? temperature = null,
            CancellationToken cancellationToken = default);
    }
}
