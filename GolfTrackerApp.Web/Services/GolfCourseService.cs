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
            var golfCourse = await _context.GolfCourses.FindAsync(id);
            if (golfCourse == null)
            {
                return false;
            }
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
    }
}