using GolfTrackerApp.Web.Models;
using GolfTrackerApp.Web.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class TeeSetServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly TeeSetService _service;

    public TeeSetServiceTests()
    {
        _service = new TeeSetService(_factory, NullLogger<TeeSetService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetTeeSetsForCourse_OrdersBySortOrderAndIncludesHoleTees()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // "Yellow", SortOrder 1
        await _service.AddTeeSetAsync(new TeeSet
        {
            GolfCourseId = course.GolfCourseId,
            Name = "Championship",
            SortOrder = 0,
        });

        var teeSets = await _service.GetTeeSetsForCourseAsync(course.GolfCourseId);

        Assert.Equal(new[] { "Championship", "Yellow" }, teeSets.Select(t => t.Name));
        Assert.Equal(18, teeSets.Single(t => t.Name == "Yellow").HoleTees.Count);
    }

    [Fact]
    public async Task AddOrUpdateHoleTee_UpdatesExistingRowInsteadOfDuplicating()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holes: 9);
        int teeSetId, holeId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            teeSetId = await context.TeeSets.Select(t => t.TeeSetId).SingleAsync();
            holeId = await context.Holes.Where(h => h.HoleNumber == 1).Select(h => h.HoleId).SingleAsync();
        }

        await _service.AddOrUpdateHoleTeeAsync(new HoleTee
        {
            HoleId = holeId, TeeSetId = teeSetId, Par = 5, StrokeIndex = 2, LengthYards = 480,
        });

        await using var verify = await _factory.CreateDbContextAsync();
        var holeTee = await verify.HoleTees.SingleAsync(ht => ht.HoleId == holeId && ht.TeeSetId == teeSetId);
        Assert.Equal(5, holeTee.Par);
        Assert.Equal(480, holeTee.LengthYards);
        Assert.Equal(9, await verify.HoleTees.CountAsync()); // still one row per hole
    }

    [Fact]
    public async Task DeleteTeeSet_UnknownId_ReturnsFalse()
    {
        Assert.False(await _service.DeleteTeeSetAsync(9999));
    }

    [Fact]
    public async Task EnsureStandardTeeSets_AddsMissingStandardTeesAndBackfillsHoleTees()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // has only "Yellow"

        await _service.EnsureStandardTeeSetsAsync(course.GolfCourseId);

        var teeSets = await _service.GetTeeSetsForCourseAsync(course.GolfCourseId);
        // Pre-existing Yellow keeps its SortOrder, which ties with the standard
        // White (both 1) — so assert membership, not order.
        Assert.Equal(new[] { "Red", "White", "Yellow" }, teeSets.Select(t => t.Name).OrderBy(n => n));
        // Every tee set has a HoleTee per hole, copied from the hole defaults.
        Assert.All(teeSets, ts => Assert.Equal(18, ts.HoleTees.Count));
        var whitePars = teeSets.Single(t => t.Name == "White").HoleTees.Select(ht => ht.Par);
        Assert.All(whitePars, par => Assert.Equal(4, par));
    }

    [Fact]
    public async Task EnsureStandardTeeSets_IsIdempotent()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory);

        await _service.EnsureStandardTeeSetsAsync(course.GolfCourseId);
        await _service.EnsureStandardTeeSetsAsync(course.GolfCourseId);

        await using var verify = await _factory.CreateDbContextAsync();
        Assert.Equal(3, await verify.TeeSets.CountAsync());
        Assert.Equal(3 * 18, await verify.HoleTees.CountAsync());
    }

    [Fact]
    public async Task SeedDefaultTeeSets_OnlyTouchesCoursesWithoutTeeSets()
    {
        var covered = await TestDataBuilder.SeedCourseAsync(
            _factory, clubName: "Covered Club"); // has "Yellow" already
        var bare = await TestDataBuilder.SeedCourseAsync(
            _factory, clubName: "Bare Club", courseName: "Bare Course");
        await using (var context = await _factory.CreateDbContextAsync())
        {
            // Strip the bare course back to holes-only (mirrors pre-tee-set legacy data).
            var bareTees = context.TeeSets.Where(t => t.GolfCourseId == bare.GolfCourseId);
            context.HoleTees.RemoveRange(
                context.HoleTees.Where(ht => bareTees.Select(t => t.TeeSetId).Contains(ht.TeeSetId)));
            context.TeeSets.RemoveRange(bareTees);
            await context.SaveChangesAsync();
        }

        await _service.SeedDefaultTeeSetsAsync();

        var bareTeeSets = await _service.GetTeeSetsForCourseAsync(bare.GolfCourseId);
        Assert.Equal(3, bareTeeSets.Count); // White, Yellow, Red created
        // Yellow copies hole yardages; White/Red are created with yardage unset.
        Assert.All(bareTeeSets.Single(t => t.Name == "Yellow").HoleTees,
            ht => Assert.NotNull(ht.LengthYards));
        Assert.All(bareTeeSets.Single(t => t.Name == "White").HoleTees,
            ht => Assert.Null(ht.LengthYards));

        var coveredTeeSets = await _service.GetTeeSetsForCourseAsync(covered.GolfCourseId);
        Assert.Single(coveredTeeSets); // untouched
    }
}
