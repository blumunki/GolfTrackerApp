using System.ComponentModel.DataAnnotations;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models
{
    public class AiAuditLog
    {
        public int AiAuditLogId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public int ResponseTimeMs { get; set; }

        [Required]
        [StringLength(50)]
        public string InsightType { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ProviderName { get; set; }

        [StringLength(50)]
        public string? ModelUsed { get; set; }

        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public bool Success { get; set; }

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        public string? PromptSent { get; set; }
        public string? ResponseReceived { get; set; }

        public int? AiChatSessionId { get; set; }
        public virtual AiChatSession? AiChatSession { get; set; }
    }
}
