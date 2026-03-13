namespace GolfTrackerApp.Mobile.Models;

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
    public string? StaleMessage { get; set; }
    public DateTime? GeneratedAt { get; set; }
}

public class AiChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class AiChatSessionSummary
{
    public int AiChatSessionId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
}

public class AiChatSessionDetail
{
    public int AiChatSessionId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public List<AiChatMessage> Messages { get; set; } = new();
}
