namespace GolfTrackerApp.Mobile.Models
{
    public class Round
    {
        public int Id { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public DateTime DatePlayed { get; set; }
        public int TotalScore { get; set; }
        public int Par { get; set; }
        public int Holes { get; set; }
        public List<string> PlayingPartners { get; set; } = new();
        public string Weather { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class Score
    {
        public int Id { get; set; }
        public int RoundId { get; set; }
        public int HoleNumber { get; set; }
        public int Strokes { get; set; }
        public int Par { get; set; }
        public int? Putts { get; set; }
        public bool FairwayHit { get; set; }
        public bool GreenInRegulation { get; set; }
    }
}
