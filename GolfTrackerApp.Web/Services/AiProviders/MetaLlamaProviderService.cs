using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services.AiProviders
{
    /// <summary>
    /// Placeholder — Meta Llama provider is not yet implemented.
    /// </summary>
    public class MetaLlamaProviderService : IAiProviderService
    {
        public string ProviderName => "MetaLlama";

        public Task<AiInsightResult> GenerateCompletionAsync(
            string systemPrompt, string userPrompt,
            int maxTokens, double temperature,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiInsightResult
            {
                Success = false,
                ErrorMessage = "MetaLlama provider is not yet configured"
            });
        }
    }
}
