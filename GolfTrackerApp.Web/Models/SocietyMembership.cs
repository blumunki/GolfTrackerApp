using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

public class SocietyMembership
{
    public int SocietyMembershipId { get; set; }

    public int GolfSocietyId { get; set; }

    [ForeignKey("GolfSocietyId")]
    public virtual GolfSociety? GolfSociety { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    public MembershipRole Role { get; set; } = MembershipRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
