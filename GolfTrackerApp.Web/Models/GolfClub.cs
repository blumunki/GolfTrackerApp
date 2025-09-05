// In GolfTrackerApp.Web/Models/GolfClub.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GolfTrackerApp.Web.Models
{
    public class GolfClub
    {
        public int GolfClubId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine1 { get; set; }

        [StringLength(100)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? CountyOrRegion { get; set; } // Or State

        [StringLength(20)]
        public string? Postcode { get; set; } // Or ZipCode

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        // Navigation property: A club can have multiple courses
        public virtual ICollection<GolfCourse> GolfCourses { get; set; } = new List<GolfCourse>();
    }
}