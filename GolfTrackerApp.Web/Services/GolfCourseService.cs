// In GolfTrackerApp.Web/Services/GolfCourseService.cs
using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class GolfCourseService : IGolfCourseService
    {
        private readonly ApplicationDbContext _context;

        public GolfCourseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GolfCourse> AddGolfCourseAsync(GolfCourse golfCourse)
        {
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
            // Include the related Holes when fetching the course
            var golfCourse = await _context.GolfCourses
                                        .Include(c => c.Holes)
                                        .FirstOrDefaultAsync(c => c.GolfCourseId == id);

            if (golfCourse == null)
            {
                return false;
            }

            // By removing the course, EF Core will automatically handle deleting the
            // associated holes due to the cascade delete relationship in the database.
            _context.GolfCourses.Remove(golfCourse);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GolfCourse>> GetAllGolfCoursesAsync()
        {
            return await _context.GolfCourses
                                .Include(gc => gc.GolfClub) // Optionally include club details
                                .ToListAsync();
        }

        public async Task<GolfCourse?> GetGolfCourseByIdAsync(int id)
        {
            return await _context.GolfCourses
                                .Include(gc => gc.GolfClub) // Optionally include club details
                                .FirstOrDefaultAsync(gc => gc.GolfCourseId == id);
        }

        public async Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse)
        {
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
            var query = _context.GolfCourses
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