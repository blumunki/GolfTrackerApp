using System.ComponentModel.DataAnnotations;

namespace GolfTrackerApp.Web.Models
{
    public class AiChatSessionMessage
    {
        public int AiChatSessionMessageId { get; set; }

        [Required]
        public int AiChatSessionId { get; set; }
        public virtual AiChatSession? AiChatSession { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
