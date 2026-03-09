using System.ComponentModel.DataAnnotations;

namespace GolfTrackerApp.Web.Models
{
    public class AiProviderSettings
    {
        public int AiProviderSettingsId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProviderName { get; set; } = string.Empty;

        public bool Enabled { get; set; }

        public int Priority { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }
    }
}
