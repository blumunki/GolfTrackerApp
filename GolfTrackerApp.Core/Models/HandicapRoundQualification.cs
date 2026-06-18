namespace GolfTrackerApp.Core.Models;

/// <summary>Why one of a player's completed rounds does or doesn't count toward their WHS index.</summary>
public enum HandicapRoundStatus
{
    /// <summary>Produced a scoring differential and counts.</summary>
    Qualified,

    /// <summary>Not an 18-hole round (v1 only handicaps full rounds).</summary>
    NotEighteenHoles,

    /// <summary>The tee played has no course rating and/or slope, so no differential can be computed.</summary>
    NoCourseRatingSlope,

    /// <summary>Fewer than 18 hole scores were recorded for the player.</summary>
    IncompleteScorecard,
}

/// <summary>One completed round and whether it qualifies for the player's handicap (with the reason).</summary>
public class HandicapRoundQualification
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int HolesPlayed { get; set; }
    public HandicapRoundStatus Status { get; set; }
    public bool Qualifies => Status == HandicapRoundStatus.Qualified;

    /// <summary>App-standard "[Club] - [Course]" label (course names like "Main Course" need the club).</summary>
    public string CourseDisplayName =>
        string.IsNullOrWhiteSpace(ClubName) ? CourseName : $"{ClubName} - {CourseName}";
}
