namespace GolfTrackerApp.Web.Models
{
    public class AiChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }
    }
}
