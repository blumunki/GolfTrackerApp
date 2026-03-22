using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

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
}
