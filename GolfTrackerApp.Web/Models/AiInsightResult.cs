namespace GolfTrackerApp.Web.Models
{
    public class AiInsightResult
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ChatSessionId { get; set; }
    }
}
