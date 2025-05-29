// In GolfTrackerApp.Web/Models/Player.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using GolfTrackerApp.Web.Data; // Assuming this is the namespace where ApplicationUser is defined

namespace GolfTrackerApp.Web.Models
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

        public double? Handicap { get; set; }

        // ApplicationUserId is now nullable
        public string? ApplicationUserId { get; set; } // Removed [Required], made nullable (string?)

        [ForeignKey("ApplicationUserId")]
        //public virtual IdentityUser? ApplicationUser { get; set; } // Navigation property remains nullable
        public virtual ApplicationUser? ApplicationUser { get; set; } // Changed to ApplicationUser, remains nullable

        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
        public virtual ICollection<RoundPlayer> RoundPlayers { get; set; } = new List<RoundPlayer>();
    }
}