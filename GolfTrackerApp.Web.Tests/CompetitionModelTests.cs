using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Tests;

public sealed class CompetitionModelTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Competition_WithEntryAndLinkedRound_RoundTrips()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId);

        int competitionId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            var competition = new Competition
            {
                Name = "Monthly Medal March 2026",
                GolfClubId = course.GolfClubId,
                GolfCourseId = course.GolfCourseId,
                ScoringFormat = ScoringFormat.Stableford,
                Date = new DateTime(2026, 3, 1),
                Status = CompetitionStatus.Upcoming,
                CreatedByUserId = TestDataBuilder.DefaultUserId,
            };
            competition.Entries.Add(new CompetitionEntry { PlayerId = player.PlayerId, HandicapAtEntry = 18.4m });
            context.Competitions.Add(competition);
            await context.SaveChangesAsync();
            competitionId = competition.CompetitionId;

            var persistedRound = await context.Rounds.FindAsync(round.RoundId);
            persistedRound!.CompetitionId = competitionId;
            await context.SaveChangesAsync();
        }

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var competition = await context.Competitions
                .Include(c => c.Entries)
                .Include(c => c.Rounds)
                .Include(c => c.GolfCourse)
                .SingleAsync(c => c.CompetitionId == competitionId);

            Assert.Equal(ScoringFormat.Stableford, competition.ScoringFormat);
            Assert.Equal(course.GolfCourseId, competition.GolfCourse!.GolfCourseId);
            var entry = Assert.Single(competition.Entries);
            Assert.Equal(18.4m, entry.HandicapAtEntry);
            var linked = Assert.Single(competition.Rounds);
            Assert.Equal(round.RoundId, linked.RoundId);
        }
    }

    [Fact]
    public async Task CompetitionEntry_DuplicatePlayer_IsRejected()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);

        await using var context = await _factory.CreateDbContextAsync();
        var competition = new Competition
        {
            Name = "Open Stableford",
            ScoringFormat = ScoringFormat.Medal,
            Date = DateTime.UtcNow.Date,
            CreatedByUserId = TestDataBuilder.DefaultUserId,
        };
        competition.Entries.Add(new CompetitionEntry { PlayerId = player.PlayerId });
        competition.Entries.Add(new CompetitionEntry { PlayerId = player.PlayerId });
        context.Competitions.Add(competition);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
