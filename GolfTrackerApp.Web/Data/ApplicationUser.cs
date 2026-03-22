using System.ComponentModel.DataAnnotations.Schema;
using GolfTrackerApp.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace GolfTrackerApp.Web.Data;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Cached FK to the user's linked Player record.
    /// Avoids the N+1 query pattern of GetPlayerByApplicationUserIdAsync on every page load.
    /// </summary>
    public int? LinkedPlayerId { get; set; }

    /// <summary>
    /// When true, AI insights are hidden for this user across all pages.
    /// </summary>
    public bool AiInsightsOptOut { get; set; }

    [ForeignKey("LinkedPlayerId")]
    public virtual Player? LinkedPlayer { get; set; }

    public virtual ICollection<ClubMembership> ClubMemberships { get; set; } = new List<ClubMembership>();
    public virtual ICollection<SocietyMembership> SocietyMemberships { get; set; } = new List<SocietyMembership>();
}

