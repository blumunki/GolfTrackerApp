using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models;

public enum TeeGender
{
    Unisex,
    Male,
    Female
}

public class TeeSet
{
    public int TeeSetId { get; set; }

    [Required]
    public int GolfCourseId { get; set; }

    [ForeignKey("GolfCourseId")]
    public virtual GolfCourse? GolfCourse { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty; // e.g. "Yellow", "White", "Red"

    [StringLength(20)]
    public string Colour { get; set; } = "#FFD700"; // Hex colour for UI

    public decimal? CourseRating { get; set; } // e.g. 71.2

    public int? SlopeRating { get; set; } // e.g. 128

    public TeeGender Gender { get; set; } = TeeGender.Unisex;

    public int SortOrder { get; set; } = 1;

    public virtual ICollection<HoleTee> HoleTees { get; set; } = new List<HoleTee>();
}
