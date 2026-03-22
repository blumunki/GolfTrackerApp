using System.Text.Json.Serialization;

namespace GolfTrackerApp.Mobile.Models;

public class GolfSocietyDto
{
    [JsonPropertyName("golfSocietyId")]
    public int GolfSocietyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }

    [JsonPropertyName("isMember")]
    public bool IsMember { get; set; }

    [JsonPropertyName("myRole")]
    public string? MyRole { get; set; }
}

public class SocietyDetailDto
{
    [JsonPropertyName("golfSocietyId")]
    public int GolfSocietyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("members")]
    public List<SocietyMemberDto> Members { get; set; } = new();

    [JsonPropertyName("myRole")]
    public string? MyRole { get; set; }

    [JsonPropertyName("isMember")]
    public bool IsMember { get; set; }
}

public class SocietyMemberDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = "Member";

    [JsonPropertyName("joinedAt")]
    public DateTime JoinedAt { get; set; }
}

public class ClubMembershipDto
{
    [JsonPropertyName("clubMembershipId")]
    public int ClubMembershipId { get; set; }

    [JsonPropertyName("golfClubId")]
    public int GolfClubId { get; set; }

    [JsonPropertyName("clubName")]
    public string? ClubName { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = "Member";

    [JsonPropertyName("membershipNumber")]
    public string? MembershipNumber { get; set; }

    [JsonPropertyName("joinedAt")]
    public DateTime JoinedAt { get; set; }
}
