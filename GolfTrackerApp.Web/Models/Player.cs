// In GolfTrackerApp.Web/Models/Player.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For ForeignKey
using GolfTrackerApp.Web.Data; // Add this using directive for ApplicationUser
using Microsoft.AspNetCore.Identity; // Required if you directly link to ApplicationUser ID

namespace GolfTrackerApp.Web.Models
{
    public class Player
    {
        public int PlayerId { get; set; } // Primary Key

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public double? Handicap { get; set; }

        // Foreign key to ApplicationUser (from ASP.NET Core Identity)
        // This links a Player profile to a registered user.
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; } // Navigation property

        // Navigation property for Scores
        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
        // Navigation property for Rounds (if tracking which players played which round through a join table)
        public virtual ICollection<RoundPlayer> RoundPlayers { get; set; } = new List<RoundPlayer>();
    }
}