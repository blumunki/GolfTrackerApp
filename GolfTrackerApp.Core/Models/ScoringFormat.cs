namespace GolfTrackerApp.Core.Models;

/// <summary>How a competition is scored. v1 scoring logic covers Medal, Stableford and MatchPlay.</summary>
public enum ScoringFormat
{
    Medal,
    Stableford,
    ModifiedStableford,
    MatchPlay,
    BetterBall,
    Scramble,
    TexasScramble,
    Fourball,
    Foursomes,
    Bogey
}
