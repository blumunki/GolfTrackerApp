// In GolfTrackerApp.Web/Models/GolfCourse.cs
using System.ComponentModel.DataAnnotations;

namespace GolfTrackerApp.Web.Models
{
    public class GolfCourse
    {
        public int GolfCourseId { get; set; } // Primary Key

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Location { get; set; }

        public int DefaultPar { get; set; } // Could be calculated or stored

        public int NumberOfHoles { get; set; } = 18; // Default to 18

        // Navigation property for Holes on this course
        public virtual ICollection<Hole> Holes { get; set; } = new List<Hole>();
        // Navigation property for Rounds played on this course
        public virtual ICollection<Round> Rounds { get; set; } = new List<Round>();
    }
}