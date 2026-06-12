using GolfTrackerApp.Core.Models;

namespace GolfTrackerApp.Core.Services;

public interface ITeeSetService
{
    Task<List<TeeSet>> GetTeeSetsForCourseAsync(int golfCourseId);
    Task<TeeSet?> GetTeeSetByIdAsync(int teeSetId);
    Task<TeeSet> AddTeeSetAsync(TeeSet teeSet);
    Task<TeeSet> UpdateTeeSetAsync(TeeSet teeSet);
    Task<bool> DeleteTeeSetAsync(int teeSetId);
    Task<HoleTee> AddOrUpdateHoleTeeAsync(HoleTee holeTee);
    Task<List<HoleTee>> GetHoleTeesForTeeSetAsync(int teeSetId);
    Task SeedDefaultTeeSetsAsync();
    Task EnsureStandardTeeSetsAsync(int golfCourseId);

    /// <summary>
    /// Creates or updates a tee set by (course, tee name — case-insensitive). Ratings are
    /// updated only when a value is provided, so a blank CSV cell never clears a manually
    /// entered rating. Colour/gender/sort order fall back to the standard-tee defaults for
    /// known tee names when creating.
    /// </summary>
    Task<TeeSet> UpsertTeeSetRatingsAsync(
        int golfCourseId, string teeName, decimal? courseRating, int? slopeRating,
        string? colour = null, TeeGender? gender = null, int? sortOrder = null);

    /// <summary>
    /// Repairs HoleTee rows created without a par (Par &lt;= 0) by copying par and stroke
    /// index from their Hole. Returns the number of rows fixed.
    /// </summary>
    Task<int> RepairHoleTeeParsAsync();
}
