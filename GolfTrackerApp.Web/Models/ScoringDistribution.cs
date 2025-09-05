namespace GolfTrackerApp.Web.Models;

public class ScoringDistribution
{
    public int EagleCount { get; set; }
    public int BirdieCount { get; set; }
    public int ParCount { get; set; }
    public int BogeyCount { get; set; }
    public int DoubleBogeyCount { get; set; }
    public int TripleBogeyOrWorseCount { get; set; }
    
    public int TotalHoles => EagleCount + BirdieCount + ParCount + BogeyCount + DoubleBogeyCount + TripleBogeyOrWorseCount;
    
    public double EaglePercentage => TotalHoles > 0 ? (double)EagleCount / TotalHoles * 100 : 0;
    public double BirdiePercentage => TotalHoles > 0 ? (double)BirdieCount / TotalHoles * 100 : 0;
    public double ParPercentage => TotalHoles > 0 ? (double)ParCount / TotalHoles * 100 : 0;
    public double BogeyPercentage => TotalHoles > 0 ? (double)BogeyCount / TotalHoles * 100 : 0;
    public double DoubleBogeyPercentage => TotalHoles > 0 ? (double)DoubleBogeyCount / TotalHoles * 100 : 0;
    public double TripleBogeyOrWorsePercentage => TotalHoles > 0 ? (double)TripleBogeyOrWorseCount / TotalHoles * 100 : 0;
}
