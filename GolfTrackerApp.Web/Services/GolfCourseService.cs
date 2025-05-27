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
            return await _context.GolfCourses.ToListAsync();
        }

        public async Task<GolfCourse?> GetGolfCourseByIdAsync(int id)
        {
            return await _context.GolfCourses.FindAsync(id);
            // Or use this to include related entities if needed later:
            // return await _context.GolfCourses.Include(gc => gc.Holes).FirstOrDefaultAsync(gc => gc.GolfCourseId == id);
        }

        public async Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse)
        {
            var existingCourse = await _context.GolfCourses.FindAsync(golfCourse.GolfCourseId);
            if (existingCourse == null)
            {
                return null; // Or throw an exception
            }

            _context.Entry(existingCourse).CurrentValues.SetValues(golfCourse);
            // Or update specific properties:
            // existingCourse.Name = golfCourse.Name;
            // existingCourse.Location = golfCourse.Location;
            // ... etc.

            await _context.SaveChangesAsync();
            return existingCourse;
        }
    }
}