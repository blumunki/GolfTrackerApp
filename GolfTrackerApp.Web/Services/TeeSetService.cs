using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class TeeSetService : ITeeSetService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<TeeSetService> _logger;

    public TeeSetService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<TeeSetService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<TeeSet>> GetTeeSetsForCourseAsync(int golfCourseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TeeSets
            .Where(ts => ts.GolfCourseId == golfCourseId)
            .Include(ts => ts.HoleTees)
            .OrderBy(ts => ts.SortOrder)
            .ToListAsync();
    }

    public async Task<TeeSet?> GetTeeSetByIdAsync(int teeSetId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TeeSets
            .Include(ts => ts.HoleTees)
            .FirstOrDefaultAsync(ts => ts.TeeSetId == teeSetId);
    }

    public async Task<TeeSet> AddTeeSetAsync(TeeSet teeSet)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.TeeSets.Add(teeSet);
        await context.SaveChangesAsync();
        return teeSet;
    }

    public async Task<TeeSet> UpdateTeeSetAsync(TeeSet teeSet)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.TeeSets.Update(teeSet);
        await context.SaveChangesAsync();
        return teeSet;
    }

    public async Task<bool> DeleteTeeSetAsync(int teeSetId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var teeSet = await context.TeeSets.FindAsync(teeSetId);
        if (teeSet == null) return false;
        context.TeeSets.Remove(teeSet);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<HoleTee> AddOrUpdateHoleTeeAsync(HoleTee holeTee)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.HoleTees
            .FirstOrDefaultAsync(ht => ht.HoleId == holeTee.HoleId && ht.TeeSetId == holeTee.TeeSetId);

        if (existing != null)
        {
            existing.Par = holeTee.Par;
            existing.StrokeIndex = holeTee.StrokeIndex;
            existing.LengthYards = holeTee.LengthYards;
        }
        else
        {
            context.HoleTees.Add(holeTee);
        }

        await context.SaveChangesAsync();
        return existing ?? holeTee;
    }

    public async Task<List<HoleTee>> GetHoleTeesForTeeSetAsync(int teeSetId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.HoleTees
            .Where(ht => ht.TeeSetId == teeSetId)
            .OrderBy(ht => ht.Hole!.HoleNumber)
            .Include(ht => ht.Hole)
            .ToListAsync();
    }

    private static readonly (string Name, string Colour, TeeGender Gender, int SortOrder)[] StandardTees = new[]
    {
        ("White", "#FFFFFF", TeeGender.Male, 1),
        ("Yellow", "#FFD700", TeeGender.Male, 2),
        ("Red", "#DC2626", TeeGender.Female, 3)
    };

    /// <summary>
    /// Creates the 3 standard tee sets (White, Yellow, Red) for every course that
    /// doesn't yet have any tee sets, copying par/SI/yardage from Hole records into the Yellow tee.
    /// </summary>
    public async Task SeedDefaultTeeSetsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var coursesWithoutTeeSets = await context.GolfCourses
            .Include(gc => gc.Holes)
            .Where(gc => !context.TeeSets.Any(ts => ts.GolfCourseId == gc.GolfCourseId))
            .Where(gc => gc.Holes.Any())
            .ToListAsync();

        if (!coursesWithoutTeeSets.Any())
        {
            _logger.LogInformation("All courses already have tee sets.");
            return;
        }

        _logger.LogInformation("Seeding standard tee sets for {Count} courses.", coursesWithoutTeeSets.Count);

        foreach (var course in coursesWithoutTeeSets)
        {
            foreach (var (name, colour, gender, sortOrder) in StandardTees)
            {
                var teeSet = new TeeSet
                {
                    GolfCourseId = course.GolfCourseId,
                    Name = name,
                    Colour = colour,
                    Gender = gender,
                    SortOrder = sortOrder
                };

                context.TeeSets.Add(teeSet);
                await context.SaveChangesAsync(); // Save to get TeeSetId

                // Copy existing hole data into Yellow tee (the original import tee)
                if (name == "Yellow")
                {
                    foreach (var hole in course.Holes)
                    {
                        context.HoleTees.Add(new HoleTee
                        {
                            HoleId = hole.HoleId,
                            TeeSetId = teeSet.TeeSetId,
                            Par = hole.Par,
                            StrokeIndex = hole.StrokeIndex,
                            LengthYards = hole.LengthYards
                        });
                    }
                }
                else
                {
                    // Create empty HoleTee records for White and Red (yardage to be filled in)
                    foreach (var hole in course.Holes)
                    {
                        context.HoleTees.Add(new HoleTee
                        {
                            HoleId = hole.HoleId,
                            TeeSetId = teeSet.TeeSetId,
                            Par = hole.Par,
                            StrokeIndex = hole.StrokeIndex,
                            LengthYards = null
                        });
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Standard tee sets seeded successfully.");
    }

    /// <summary>
    /// Ensures the 3 standard tee sets (White, Yellow, Red) exist for a specific course.
    /// Called after course creation.
    /// </summary>
    public async Task EnsureStandardTeeSetsAsync(int golfCourseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingTees = await context.TeeSets
            .Where(ts => ts.GolfCourseId == golfCourseId)
            .ToListAsync();

        var existingTeeNames = existingTees.Select(ts => ts.Name).ToHashSet();

        var newTeeSets = new List<TeeSet>();
        foreach (var (name, colour, gender, sortOrder) in StandardTees)
        {
            if (existingTeeNames.Contains(name)) continue;

            var teeSet = new TeeSet
            {
                GolfCourseId = golfCourseId,
                Name = name,
                Colour = colour,
                Gender = gender,
                SortOrder = sortOrder
            };
            context.TeeSets.Add(teeSet);
            newTeeSets.Add(teeSet);
        }

        if (newTeeSets.Any())
            await context.SaveChangesAsync();

        // Backfill missing HoleTee records for ALL tee sets (existing + new)
        var allTeeSets = existingTees.Concat(newTeeSets).ToList();
        var holes = await context.Holes
            .Where(h => h.GolfCourseId == golfCourseId)
            .Select(h => new { h.HoleId, h.Par, h.StrokeIndex })
            .ToListAsync();

        if (!holes.Any()) return;

        var existingHoleTees = await context.HoleTees
            .Where(ht => allTeeSets.Select(ts => ts.TeeSetId).Contains(ht.TeeSetId))
            .Select(ht => new { ht.HoleId, ht.TeeSetId })
            .ToListAsync();

        var existingHoleTeeKeys = existingHoleTees.Select(x => (x.HoleId, x.TeeSetId)).ToHashSet();

        var added = false;
        foreach (var ts in allTeeSets)
        {
            foreach (var hole in holes)
            {
                if (existingHoleTeeKeys.Contains((hole.HoleId, ts.TeeSetId))) continue;

                context.HoleTees.Add(new HoleTee
                {
                    HoleId = hole.HoleId,
                    TeeSetId = ts.TeeSetId,
                    Par = hole.Par,
                    StrokeIndex = hole.StrokeIndex
                });
                added = true;
            }
        }

        if (added)
            await context.SaveChangesAsync();
    }
}
