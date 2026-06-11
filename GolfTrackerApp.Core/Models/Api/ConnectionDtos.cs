namespace GolfTrackerApp.Web.Models.Api;

public class ConnectionDto
{
    public int Id { get; set; }
    public string ConnectedUserId { get; set; } = string.Empty;
    public string ConnectedUserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ConnectedSince { get; set; }
    public DateTime? RequestedAt { get; set; }
}

public class ConnectionRequestDto
{
    public string TargetUserId { get; set; } = string.Empty;
}

public class MergeRequestDto
{
    public int SourcePlayerId { get; set; }
    public string TargetUserId { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class MergeResponseDto
{
    public int Id { get; set; }
    public string? RequestingUserName { get; set; }
    public string? TargetUserName { get; set; }
    public string SourcePlayerName { get; set; } = string.Empty;
    public string TargetPlayerName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
