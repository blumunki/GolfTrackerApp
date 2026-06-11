namespace GolfTrackerApp.Web.Models;

public class PlayingPartnerSummary
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public DateTime LastPlayedDate { get; set; }
    public int UserWins { get; set; }
    public int PartnerWins { get; set; }
    public int Ties { get; set; }
}