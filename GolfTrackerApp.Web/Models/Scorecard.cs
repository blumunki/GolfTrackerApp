namespace GolfTrackerApp.Web.Models;

// This class will hold all the necessary data for the scorecard UI
public class Scorecard
{
    public List<Player> Players { get; set; } = new();
    public List<Hole> PlayedHoles { get; set; } = new();
    public Dictionary<int, List<HoleScoreEntryModel>> Scores { get; set; } = new();
}