using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models;

public class HoleTee
{
    public int HoleTeeId { get; set; }

    [Required]
    public int HoleId { get; set; }

    [ForeignKey("HoleId")]
    public virtual Hole? Hole { get; set; }

    [Required]
    public int TeeSetId { get; set; }

    [ForeignKey("TeeSetId")]
    public virtual TeeSet? TeeSet { get; set; }

    [Required]
    public int Par { get; set; }

    public int? StrokeIndex { get; set; }

    public int? LengthYards { get; set; }
}
