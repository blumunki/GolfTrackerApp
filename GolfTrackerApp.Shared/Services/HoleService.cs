// In GolfTrackerApp.Web/Services/HoleService.cs
using GolfTrackerApp.Shared.Data;
using GolfTrackerApp.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Shared.Services
{
    public class HoleService : IHoleService
    {
        private readonly ApplicationDbContext _context;

        public HoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Hole> AddHoleAsync(Hole hole)
        {
            // Ensure GolfCourseId is valid
            if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == hole.GolfCourseId))
            {
                throw new ArgumentException($"GolfCourse with ID {hole.GolfCourseId} does not exist.");
            }
            _context.Holes.Add(hole);
            await _context.SaveChangesAsync();
            return hole;
        }

        public async Task<List<Hole>> AddHolesAsync(IEnumerable<Hole> holes)
        {
            // Optional: Add validation for each hole in the list
            _context.Holes.AddRange(holes);
            await _context.SaveChangesAsync();
            return holes.ToList();
        }

        public async Task<bool> DeleteHoleAsync(int id)
        {
            var hole = await _context.Holes.FindAsync(id);
            if (hole == null) return false;
            _context.Holes.Remove(hole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Hole?> GetHoleByIdAsync(int id)
        {
            return await _context.Holes.Include(h => h.GolfCourse).FirstOrDefaultAsync(h => h.HoleId == id);
        }

        public async Task<List<Hole>> GetHolesForCourseAsync(int golfCourseId)
        {
            return await _context.Holes
                                 .Where(h => h.GolfCourseId == golfCourseId)
                                 .OrderBy(h => h.HoleNumber)
                                 .ToListAsync();
        }

        public async Task<Hole?> UpdateHoleAsync(Hole hole)
        {
            var existingHole = await _context.Holes.FindAsync(hole.HoleId);
            if (existingHole == null) return null;

            if (existingHole.GolfCourseId != hole.GolfCourseId)
            {
                if (!await _context.GolfCourses.AnyAsync(gc => gc.GolfCourseId == hole.GolfCourseId))
                {
                    throw new ArgumentException($"GolfCourse with ID {hole.GolfCourseId} does not exist for update.");
                }
            }
            _context.Entry(existingHole).CurrentValues.SetValues(hole);
            await _context.SaveChangesAsync();
            return existingHole;
        }
    }
}