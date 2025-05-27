// In GolfTrackerApp.Web/Models/Round.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models
{
    public class Round
    {
        public int RoundId { get; set; } // Primary Key

        [Required]
        public int GolfCourseId { get; set; } // Foreign Key
        [ForeignKey("GolfCourseId")]
        public virtual GolfCourse? GolfCourse { get; set; } // Navigation property

        [Required]
        public DateTime DatePlayed { get; set; }

        public string? Notes { get; set; } // e.g., weather, conditions

        // Navigation property for Scores in this round
        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
        // Navigation property for Players in this round (through a join table)
        public virtual ICollection<RoundPlayer> RoundPlayers { get; set; } = new List<RoundPlayer>();
    }
}