// In GolfTrackerApp.Web/Models/Hole.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models
{
    public class Hole
    {
        public int HoleId { get; set; } // Primary Key

        [Required]
        public int GolfCourseId { get; set; } // Foreign Key
        [ForeignKey("GolfCourseId")]
        public virtual GolfCourse? GolfCourse { get; set; } // Navigation property

        [Required]
        public int HoleNumber { get; set; } // e.g., 1 through 18

        [Required]
        public int Par { get; set; }

        public int? StrokeIndex { get; set; }
        public int? LengthYards { get; set; }

        // Navigation property for Scores on this hole
        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();
    }
}