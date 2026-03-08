using System.ComponentModel.DataAnnotations;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models
{
    public class AiChatSession
    {
        public int AiChatSessionId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; }

        public virtual ICollection<AiChatSessionMessage> Messages { get; set; } = new List<AiChatSessionMessage>();
        public virtual ICollection<AiAuditLog> AuditLogs { get; set; } = new List<AiAuditLog>();
    }
}
