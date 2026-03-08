using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services
{
    public interface IAiProviderService
    {
        string ProviderName { get; }
        Task<AiInsightResult> GenerateCompletionAsync(
            string systemPrompt,
            string userPrompt,
            int maxTokens,
            double temperature,
            CancellationToken cancellationToken = default);
    }
}
