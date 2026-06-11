// In GolfTrackerApp.Web/Models/Player.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using GolfTrackerApp.Core.Data; // Assuming this is the namespace where ApplicationUser is defined

namespace GolfTrackerApp.Core.Models
{
    public class Player
    {
        public int PlayerId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        // Display/convenience value: the "active" handicap from the primary source.
        public double? Handicap { get; set; }

        // Which handicap context is shown as "my handicap" (null = legacy manual value).
        public HandicapSource? PrimaryHandicapSource { get; set; }

        // ApplicationUserId is now nullable
        public string? ApplicationUserId { get; set; } // Removed [Required], made nullable (string?)

        [ForeignKey("ApplicationUserId")]
        //public virtual IdentityUser? ApplicationUser { get; set; } // Navigation property remains nullable
        public virtual ApplicationUser? ApplicationUser { get; set; } // Changed to ApplicationUser, remains nullable

        // Owner of this player record (especially for managed players)
        public string CreatedByApplicationUserId { get; set; } = string.Empty; // No longer nullable
        [ForeignKey("CreatedByApplicationUserId")]
        public virtual ApplicationUser? CreatedByApplicationUser { get; set; }

        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
        public virtual ICollection<RoundPlayer> RoundPlayers { get; set; } = new List<RoundPlayer>();
        public virtual ICollection<HandicapRecord> HandicapRecords { get; set; } = new List<HandicapRecord>();
    }
}