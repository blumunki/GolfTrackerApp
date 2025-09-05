namespace GolfTrackerApp.Web.Models;

public class PerformanceByPar
{
    public double Par3Average { get; set; }
    public double Par4Average { get; set; }
    public double Par5Average { get; set; }
    
    public int Par3Count { get; set; }
    public int Par4Count { get; set; }
    public int Par5Count { get; set; }
    
    // Relative to par calculations
    public double Par3RelativeToPar => Par3Average - 3;
    public double Par4RelativeToPar => Par4Average - 4;
    public double Par5RelativeToPar => Par5Average - 5;
    
    public bool HasValidData => Par3Count > 0 || Par4Count > 0 || Par5Count > 0;
}
