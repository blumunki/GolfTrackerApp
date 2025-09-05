namespace GolfTrackerApp.Mobile.Models;

public class GolfClub
{
    public int GolfClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? CountyOrRegion { get; set; }
    public string? Postcode { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
}
