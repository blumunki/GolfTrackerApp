namespace GolfTrackerApp.Web.Models;

/// <summary>
/// Contains comparison data for multiple players across shared rounds or all rounds.
/// </summary>
public class PlayerComparisonResult
{
    public List<PlayerComparisonSeries> PlayerSeries { get; set; } = new();
    public List<PlayerComparisonSummary> Summaries { get; set; } = new();
}

/// <summary>
/// Performance data series for a single player in a comparison view.
/// </summary>
public class PlayerComparisonSeries
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public List<PlayerPerformanceDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Summary stats for a player in a comparison context.
/// </summary>
public class PlayerComparisonSummary
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public double AverageScore { get; set; }
    public double AverageToPar { get; set; }
    public int BestScore { get; set; }
    public int BestToPar { get; set; }
    public int SharedRounds { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
}

/// <summary>
/// Enriched round detail for drill-down when user clicks a performance chart data point.
/// </summary>
public class RoundDetailSummary
{
    public int RoundId { get; set; }
    public DateTime DatePlayed { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string ClubName { get; set; } = string.Empty;
    public int HolesPlayed { get; set; }
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int ScoreVsPar => TotalScore - TotalPar;
    public string RoundType { get; set; } = string.Empty;
    public List<RoundDetailScore> HoleScores { get; set; } = new();
    public List<RoundDetailPlayer> OtherPlayers { get; set; } = new();
}

/// <summary>
/// Individual hole score within a round drill-down.
/// </summary>
public class RoundDetailScore
{
    public int HoleNumber { get; set; }
    public int Par { get; set; }
    public int Strokes { get; set; }
    public int ScoreVsPar => Strokes - Par;
    public string ScoreLabel => ScoreVsPar switch
    {
        <= -2 => "Eagle",
        -1 => "Birdie",
        0 => "Par",
        1 => "Bogey",
        2 => "Double",
        _ => $"+{ScoreVsPar}"
    };
}

/// <summary>
/// Other player who was in the same round (for drill-down context).
/// </summary>
public class RoundDetailPlayer
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int TotalPar { get; set; }
    public int ScoreVsPar => TotalScore - TotalPar;
}
