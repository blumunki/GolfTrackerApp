using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Data;

namespace GolfTrackerApp.Web.Models;

public class ClubMembership
{
    public int ClubMembershipId { get; set; }

    public int GolfClubId { get; set; }

    [ForeignKey("GolfClubId")]
    public virtual GolfClub? GolfClub { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    public MembershipRole Role { get; set; } = MembershipRole.Member;

    [StringLength(50)]
    public string? MembershipNumber { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
