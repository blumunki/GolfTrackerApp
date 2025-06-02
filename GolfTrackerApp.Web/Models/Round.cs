// In GolfTrackerApp.Web/Models/Round.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models
{
    public enum RoundTypeOption
    {
        Friendly,
        Competitive
        // Add more if needed
    }
    public class Round
    {
        public int RoundId { get; set; } // Primary Key

        [Required]
        public int GolfCourseId { get; set; } // Foreign Key
        [ForeignKey("GolfCourseId")]
        public virtual GolfCourse? GolfCourse { get; set; } // Navigation property

        [Required]
        public DateTime DatePlayed { get; set; }

        // New Fields
        [Required]
        public int StartingHole { get; set; } = 1; // Default to 1

        [Required]
        [Range(1, 18)] // Assuming a course won't have more than 100 holes (e.g. 27 hole courses playing 3*9 etc)
        public int HolesPlayed { get; set; } = 18; // Default to 18

        [Required]
        [StringLength(50)]
        public RoundTypeOption RoundType { get; set; } = RoundTypeOption.Friendly; // Or use the Enum: public RoundTypeOption Type {get; set;}

        public string? Notes { get; set; } // e.g., weather, conditions

        [Required]
        public string CreatedByApplicationUserId { get; set; } = string.Empty; // No longer nullable
        [ForeignKey("CreatedByApplicationUserId")]
        public virtual ApplicationUser? CreatedByApplicationUser { get; set; }

        // Navigation property for Scores in this round
        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
        // Navigation property for Players in this round (through a join table)
        public virtual ICollection<RoundPlayer> RoundPlayers { get; set; } = new List<RoundPlayer>();
    }
}