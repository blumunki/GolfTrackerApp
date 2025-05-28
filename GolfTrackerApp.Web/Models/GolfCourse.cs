// In GolfTrackerApp.Web/Models/GolfCourse.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For ForeignKey

namespace GolfTrackerApp.Web.Models
{
    public class GolfCourse
    {
        public int GolfCourseId { get; set; } // Primary Key

        [Required]
        public int GolfClubId { get; set; } // Foreign Key to GolfClub
        [ForeignKey("GolfClubId")]
        public virtual GolfClub? GolfClub { get; set; } // Navigation property

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "The Old Course", "PGA National"

        // Removed Location string as it's now part of GolfClub
        // public string? Location { get; set; }

        public int DefaultPar { get; set; }
        public int NumberOfHoles { get; set; } = 18;

        public virtual ICollection<Hole> Holes { get; set; } = new List<Hole>();
        public virtual ICollection<Round> Rounds { get; set; } = new List<Round>();
    }
}