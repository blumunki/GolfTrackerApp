using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

/// <summary>
/// Smoke tests proving the test infrastructure works end to end:
/// in-memory SQLite schema creation, data seeding, and real services
/// running against the factory.
/// </summary>
public sealed class SmokeTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SeededCourseGraph_RoundTripsThroughTheDatabase()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holes: 18, parPerHole: 4);

        await using var context = await _factory.CreateDbContextAsync();
        var loaded = await context.GolfCourses
            .Include(c => c.GolfClub)
            .Include(c => c.Holes)
            .Include(c => c.TeeSets)
                .ThenInclude(t => t.HoleTees)
            .SingleAsync(c => c.GolfCourseId == course.GolfCourseId);

        Assert.Equal("Test Golf Club", loaded.GolfClub!.Name);
        Assert.Equal(18, loaded.Holes.Count);
        var teeSet = Assert.Single(loaded.TeeSets);
        Assert.Equal(70.0m, teeSet.CourseRating);
        Assert.Equal(120, teeSet.SlopeRating);
        Assert.Equal(18, teeSet.HoleTees.Count);
    }

    [Fact]
    public async Task GolfClubService_AddAndGetAll_ReturnsSavedClub()
    {
        var service = new GolfClubService(_factory, NullLogger<GolfClubService>.Instance);

        await service.AddGolfClubAsync(new GolfClub { Name = "Smoke Test GC", Country = "Ireland" });
        var clubs = await service.GetAllGolfClubsAsync();

        var club = Assert.Single(clubs);
        Assert.Equal("Smoke Test GC", club.Name);
    }

    [Fact]
    public async Task RoundService_AddRoundAsync_LinksPlayersWithTeeSelections()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var teeSetId = await context.TeeSets.Select(t => t.TeeSetId).SingleAsync();

            var service = new RoundService(_factory, NullLogger<RoundService>.Instance, new HandicapService(_factory, NullLogger<HandicapService>.Instance));
            var round = await service.AddRoundAsync(
                new Round
                {
                    GolfCourseId = course.GolfCourseId,
                    DatePlayed = DateTime.UtcNow.Date,
                    CreatedByApplicationUserId = TestDataBuilder.DefaultUserId,
                },
                new[] { player.PlayerId },
                new Dictionary<int, int?> { [player.PlayerId] = teeSetId });

            Assert.True(round.RoundId > 0);
        }

        await using (var verify = await _factory.CreateDbContextAsync())
        {
            var roundPlayer = await verify.RoundPlayers.SingleAsync();
            Assert.Equal(player.PlayerId, roundPlayer.PlayerId);
            Assert.NotNull(roundPlayer.TeeSetId);
        }
    }
}
