// In GolfTrackerApp.Web/Models/Score.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfTrackerApp.Web.Models
{
    public class Score
    {
        public int ScoreId { get; set; } // Primary Key

        [Required]
        public int RoundId { get; set; } // Foreign Key
        [ForeignKey("RoundId")]
        public virtual Round? Round { get; set; } // Navigation property

        [Required]
        public int PlayerId { get; set; } // Foreign Key
        [ForeignKey("PlayerId")]
        public virtual Player? Player { get; set; } // Navigation property

        [Required]
        public int HoleId { get; set; } // Foreign Key
        [ForeignKey("HoleId")]
        public virtual Hole? Hole { get; set; } // Navigation property

        [Required]
        public int Strokes { get; set; }

        public int? Putts { get; set; }
        public bool? FairwayHit { get; set; }
    }
}