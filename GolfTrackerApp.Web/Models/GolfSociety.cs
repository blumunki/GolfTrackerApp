using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

public class GolfSociety
{
    public int GolfSocietyId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    [ForeignKey("CreatedByUserId")]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<SocietyMembership> Memberships { get; set; } = new List<SocietyMembership>();
}
