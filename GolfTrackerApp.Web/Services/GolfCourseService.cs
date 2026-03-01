using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class GolfCourseService : IGolfCourseService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<GolfCourseService> _logger;

        public GolfCourseService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<GolfCourseService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<GolfCourse> AddGolfCourseAsync(GolfCourse golfCourse)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // You might want to add validation here to ensure golfCourse.GolfClubId refers to an existing GolfClub
            var clubExists = await _context.GolfClubs.AnyAsync(c => c.GolfClubId == golfCourse.GolfClubId);
            if (!clubExists)
            {
                throw new ArgumentException($"GolfClub with ID {golfCourse.GolfClubId} does not exist.");
            }

            _context.GolfCourses.Add(golfCourse);
            await _context.SaveChangesAsync();
            return golfCourse;
        }

        public async Task<bool> DeleteGolfCourseAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var hasRounds = await _context.Rounds.AnyAsync(r => r.GolfCourseId == id);
            if (hasRounds)
            {
                _logger.LogWarning("Cannot delete GolfCourse {CourseId}: has linked rounds", id);
                throw new InvalidOperationException($"Cannot delete this course because it has linked rounds. Remove the rounds first.");
            }

            var golfCourse = await _context.GolfCourses
                                        .Include(c => c.Holes)
                                        .FirstOrDefaultAsync(c => c.GolfCourseId == id);

            if (golfCourse == null)
            {
                return false;
            }

            // EF Core cascade-deletes the associated holes.
            _context.GolfCourses.Remove(golfCourse);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GolfCourse>> GetAllGolfCoursesAsync()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.GolfCourses
                                .AsNoTracking()
                                .Include(gc => gc.GolfClub)
                                .ToListAsync();
        }

        public async Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.GolfCourses
                .AsNoTracking()
                .Where(c => c.GolfClubId == clubId)
                .ToListAsync();
        }

        public async Task<GolfCourse?> GetGolfCourseByIdAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.GolfCourses
                                .AsNoTracking()
                                .Include(gc => gc.GolfClub)
                                .Include(gc => gc.Holes)
                                .FirstOrDefaultAsync(gc => gc.GolfCourseId == id);
        }

        public async Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var existingCourse = await _context.GolfCourses.FindAsync(golfCourse.GolfCourseId);
            if (existingCourse == null)
            {
                return null;
            }

            // Ensure GolfClubId is valid if it's being changed
            if (existingCourse.GolfClubId != golfCourse.GolfClubId)
            {
                var clubExists = await _context.GolfClubs.AnyAsync(c => c.GolfClubId == golfCourse.GolfClubId);
                if (!clubExists)
                {
                    throw new ArgumentException($"GolfClub with ID {golfCourse.GolfClubId} does not exist for update.");
                }
            }

            _context.Entry(existingCourse).CurrentValues.SetValues(golfCourse);
            await _context.SaveChangesAsync();
            return existingCourse;
        }
        public async Task<List<GolfCourse>> SearchGolfCoursesAsync(string searchTerm)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var query = _context.GolfCourses
                .AsNoTracking()
                .Include(gc => gc.GolfClub)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                query = query.Where(c => 
                    EF.Functions.Like(c.Name, pattern) ||
                    (c.GolfClub != null && EF.Functions.Like(c.GolfClub.Name, pattern))
                );
            }

            return await query.OrderBy(c => c.GolfClub!.Name).ThenBy(c => c.Name).ToListAsync();
        }
    }
}