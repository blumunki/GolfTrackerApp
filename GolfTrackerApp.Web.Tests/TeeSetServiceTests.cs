using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
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

    [Fact]
    public async Task UpsertTeeSetRatings_UpdatesExistingTeeCaseInsensitively_WithoutTouchingColour()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // seeds "Yellow", colour #FFD700
        var originalColour = (await _service.GetTeeSetsForCourseAsync(course.GolfCourseId)).Single().Colour;

        var updated = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "yellow", 67.3m, 113);

        Assert.Equal(67.3m, updated.CourseRating);
        Assert.Equal(113, updated.SlopeRating);
        Assert.Equal(originalColour, updated.Colour);
        Assert.Single(await _service.GetTeeSetsForCourseAsync(course.GolfCourseId)); // no duplicate
    }

    [Fact]
    public async Task UpsertTeeSetRatings_BlankValuesNeverClearExistingRatings()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // Yellow: CR 70.0, slope 120
        var result = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "Yellow", null, null);

        Assert.Equal(70.0m, result.CourseRating);
        Assert.Equal(120, result.SlopeRating);
    }

    [Fact]
    public async Task UpsertTeeSetRatings_CreatesMissingTeeWithStandardDefaults()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // only Yellow exists

        var created = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "Red", 71.8m, 121);

        Assert.Equal("#DC2626", created.Colour); // standard Red colour derived from name
        Assert.Equal(TeeGender.Female, created.Gender);
        Assert.Equal(3, created.SortOrder);
        Assert.Equal(71.8m, created.CourseRating);
    }

    [Fact]
    public async Task UpsertTeeSetRatings_NonStandardTeeName_GetsNeutralDefaults()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // Yellow has SortOrder 1

        var created = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "Championship", 73.0m, 135);

        Assert.Equal("#CCCCCC", created.Colour);
        Assert.Equal(TeeGender.Unisex, created.Gender);
        Assert.Equal(2, created.SortOrder); // max existing + 1
    }

    [Fact]
    public async Task UpsertTeeSetRatings_ExplicitValuesOverrideDefaults()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory);

        var created = await _service.UpsertTeeSetRatingsAsync(
            course.GolfCourseId, "Red", 71.8m, 121, colour: "#FF0000", gender: TeeGender.Unisex, sortOrder: 9);

        Assert.Equal("#FF0000", created.Colour);
        Assert.Equal(TeeGender.Unisex, created.Gender);
        Assert.Equal(9, created.SortOrder);
    }

    [Fact]
    public async Task UpsertTeeSetRatings_UnknownCourse_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpsertTeeSetRatingsAsync(9999, "Yellow", 67.3m, 113));
    }

    [Fact]
    public async Task AddOrUpdateHoleTee_WithoutPar_InheritsParAndStrokeIndexFromHole()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory, holePars: new[] { 5, 4, 4, 3, 4, 4, 5, 3, 4, 4, 4, 3, 5, 4, 4, 3, 4, 5 });
        await using var context = await _factory.CreateDbContextAsync();
        var hole1 = await context.Holes.SingleAsync(h => h.GolfCourseId == course.GolfCourseId && h.HoleNumber == 1);
        var white = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "White", 68.1m, 116);

        var holeTee = await _service.AddOrUpdateHoleTeeAsync(new HoleTee
        {
            HoleId = hole1.HoleId,
            TeeSetId = white.TeeSetId,
            LengthYards = 410, // yardage-only, like the CSV import
        });

        Assert.Equal(5, holeTee.Par); // inherited from hole, not 0
        Assert.Equal(hole1.StrokeIndex, holeTee.StrokeIndex);
    }

    [Fact]
    public async Task DeleteTeeSet_RemovesItsHoleTees_ButIsBlockedWhenRoundsReferenceIt()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory); // Yellow + 18 hole tees

        int yellowId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            yellowId = (await context.TeeSets.SingleAsync()).TeeSetId;
        }

        // Unreferenced tee set deletes cleanly, taking its hole tees with it.
        Assert.True(await _service.DeleteTeeSetAsync(yellowId));
        await using (var context = await _factory.CreateDbContextAsync())
        {
            Assert.Equal(0, await context.HoleTees.CountAsync());
        }

        // A tee set referenced by round data must not be deletable.
        var white = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "White", 68.1m, 116);
        await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, teeSetId: white.TeeSetId);

        await Assert.ThrowsAsync<DbUpdateException>(() => _service.DeleteTeeSetAsync(white.TeeSetId));
    }

    [Fact]
    public async Task RepairHoleTeePars_FixesZeroParRowsAndReportsCount()
    {
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var white = await _service.UpsertTeeSetRatingsAsync(course.GolfCourseId, "White", 68.1m, 116);

        // Simulate historical import damage: par-0 hole tees written directly.
        await using (var context = await _factory.CreateDbContextAsync())
        {
            var holeIds = await context.Holes
                .Where(h => h.GolfCourseId == course.GolfCourseId)
                .Select(h => h.HoleId)
                .Take(3)
                .ToListAsync();
            foreach (var holeId in holeIds)
            {
                context.HoleTees.Add(new HoleTee { HoleId = holeId, TeeSetId = white.TeeSetId, LengthYards = 300 });
            }
            await context.SaveChangesAsync();
        }

        var repaired = await _service.RepairHoleTeeParsAsync();

        Assert.Equal(3, repaired);
        await using (var verify = await _factory.CreateDbContextAsync())
        {
            Assert.Equal(0, await verify.HoleTees.CountAsync(ht => ht.Par <= 0));
        }
    }
}
