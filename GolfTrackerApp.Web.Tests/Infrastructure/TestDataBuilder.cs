using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Tests.Infrastructure;

/// <summary>
/// Seeds entity graphs for tests. Shapes mirror the CSV seed data in
/// GolfTrackerApp.Web/Data (clubs → courses → holes/tee sets, rounds → scores).
/// </summary>
public static class TestDataBuilder
{
    public const string DefaultUserId = "test-user-1";

    /// <summary>Creates the ApplicationUser that owns players and rounds.</summary>
    public static async Task<ApplicationUser> SeedUserAsync(
        IDbContextFactory<ApplicationDbContext> factory, string userId = DefaultUserId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@example.com",
            NormalizedUserName = $"{userId}@EXAMPLE.COM",
            Email = $"{userId}@example.com",
            NormalizedEmail = $"{userId}@EXAMPLE.COM",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<Player> SeedPlayerAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        string firstName = "Test",
        string lastName = "Player",
        double? handicap = null,
        string createdByUserId = DefaultUserId,
        string? linkedUserId = null)
    {
        await using var context = await factory.CreateDbContextAsync();
        var player = new Player
        {
            FirstName = firstName,
            LastName = lastName,
            Handicap = handicap,
            CreatedByApplicationUserId = createdByUserId,
            ApplicationUserId = linkedUserId,
        };
        context.Players.Add(player);
        await context.SaveChangesAsync();
        return player;
    }

    /// <summary>
    /// Seeds a club with one course, <paramref name="holes"/> holes (par 4 by default,
    /// stroke index = hole number) and one tee set with per-hole tee data.
    /// Pass <paramref name="holePars"/> (length = <paramref name="holes"/>) for a mixed-par layout.
    /// </summary>
    public static async Task<GolfCourse> SeedCourseAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        string clubName = "Test Golf Club",
        string courseName = "Main Course",
        int holes = 18,
        int parPerHole = 4,
        string teeName = "Yellow",
        decimal? courseRating = 70.0m,
        int? slopeRating = 120,
        int[]? holePars = null)
    {
        if (holePars is not null && holePars.Length != holes)
        {
            throw new ArgumentException($"holePars must have {holes} entries.", nameof(holePars));
        }

        await using var context = await factory.CreateDbContextAsync();

        var club = new GolfClub { Name = clubName };
        var course = new GolfCourse
        {
            GolfClub = club,
            Name = courseName,
            NumberOfHoles = holes,
            DefaultPar = holePars?.Sum() ?? parPerHole * holes,
        };
        var teeSet = new TeeSet
        {
            GolfCourse = course,
            Name = teeName,
            CourseRating = courseRating,
            SlopeRating = slopeRating,
            SortOrder = 1,
        };

        for (var n = 1; n <= holes; n++)
        {
            var par = holePars?[n - 1] ?? parPerHole;
            var hole = new Hole
            {
                GolfCourse = course,
                HoleNumber = n,
                Par = par,
                StrokeIndex = n,
                LengthYards = 300 + n * 10,
            };
            hole.HoleTees.Add(new HoleTee
            {
                TeeSet = teeSet,
                Par = par,
                StrokeIndex = n,
                LengthYards = 300 + n * 10,
            });
            course.Holes.Add(hole);
        }

        context.GolfClubs.Add(club);
        context.TeeSets.Add(teeSet);
        await context.SaveChangesAsync();
        return course;
    }

    /// <summary>
    /// Seeds a completed round for one player with a score on every hole.
    /// <paramref name="strokesPerHole"/> defaults to one over par (bogey golf).
    /// </summary>
    public static async Task<Round> SeedCompletedRoundAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        int courseId,
        int playerId,
        DateTime? datePlayed = null,
        int? strokesPerHole = null,
        int? teeSetId = null,
        string createdByUserId = DefaultUserId)
    {
        await using var context = await factory.CreateDbContextAsync();

        var holes = await context.Holes
            .Where(h => h.GolfCourseId == courseId)
            .OrderBy(h => h.HoleNumber)
            .ToListAsync();

        var round = new Round
        {
            GolfCourseId = courseId,
            DatePlayed = datePlayed ?? DateTime.UtcNow.Date,
            StartingHole = 1,
            HolesPlayed = holes.Count,
            RoundType = RoundTypeOption.Friendly,
            Status = RoundCompletionStatus.Completed,
            CreatedByApplicationUserId = createdByUserId,
        };
        round.RoundPlayers.Add(new RoundPlayer { PlayerId = playerId, TeeSetId = teeSetId });

        foreach (var hole in holes)
        {
            round.Scores.Add(new Score
            {
                PlayerId = playerId,
                HoleId = hole.HoleId,
                Strokes = strokesPerHole ?? hole.Par + 1,
                TeeSetId = teeSetId,
            });
        }

        context.Rounds.Add(round);
        await context.SaveChangesAsync();
        return round;
    }

    /// <summary>
    /// Seeds a round for any number of players with explicit per-hole strokes.
    /// <paramref name="strokesByPlayer"/> maps playerId → strokes per hole, in hole-number
    /// order; the array length must match the course's hole count.
    /// </summary>
    public static async Task<Round> SeedRoundAsync(
        IDbContextFactory<ApplicationDbContext> factory,
        int courseId,
        IReadOnlyDictionary<int, int[]> strokesByPlayer,
        DateTime? datePlayed = null,
        RoundCompletionStatus status = RoundCompletionStatus.Completed,
        string createdByUserId = DefaultUserId)
    {
        await using var context = await factory.CreateDbContextAsync();

        var holes = await context.Holes
            .Where(h => h.GolfCourseId == courseId)
            .OrderBy(h => h.HoleNumber)
            .ToListAsync();

        var round = new Round
        {
            GolfCourseId = courseId,
            DatePlayed = datePlayed ?? DateTime.UtcNow.Date,
            StartingHole = 1,
            HolesPlayed = holes.Count,
            RoundType = RoundTypeOption.Friendly,
            Status = status,
            CreatedByApplicationUserId = createdByUserId,
        };

        foreach (var (playerId, strokes) in strokesByPlayer)
        {
            if (strokes.Length != holes.Count)
            {
                throw new ArgumentException(
                    $"Player {playerId}: expected {holes.Count} stroke entries, got {strokes.Length}.",
                    nameof(strokesByPlayer));
            }

            round.RoundPlayers.Add(new RoundPlayer { PlayerId = playerId });
            for (var i = 0; i < holes.Count; i++)
            {
                round.Scores.Add(new Score
                {
                    PlayerId = playerId,
                    HoleId = holes[i].HoleId,
                    Strokes = strokes[i],
                });
            }
        }

        context.Rounds.Add(round);
        await context.SaveChangesAsync();
        return round;
    }
}
