namespace GolfTrackerApp.Shared.Models;

public class HoleScoreEntryModel
{
    public int HoleId { get; set; }
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int StrokeIndex { get; set; }
    public int? LengthYards { get; set; }
    public int? Strokes { get; set; }
    public int? Putts { get; set; }
    public bool? FairwayHit { get; set; }
    public int? ScoreVsPar => Strokes.HasValue && Par != 0 ? Strokes - Par : null;
}