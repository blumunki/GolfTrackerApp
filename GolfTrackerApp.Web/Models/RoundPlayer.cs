// In GolfTrackerApp.Web/Models/RoundPlayer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models
{
    public class RoundPlayer
    {
        // Composite Primary Key will be configured in ApplicationDbContext
        [Required]
        public int RoundId { get; set; }
        [ForeignKey("RoundId")]
        public virtual Round? Round { get; set; }

        [Required]
        public int PlayerId { get; set; }
        [ForeignKey("PlayerId")]
        public virtual Player? Player { get; set; }

        // You can add properties specific to a player in a particular round here
        // e.g., public int? OverallScoreForRound { get; set; }
        // However, calculating this might be better than storing it directly
    }
}