// In GolfTrackerApp.Web/Models/RoundPlayer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Shared.Models
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
    }
}