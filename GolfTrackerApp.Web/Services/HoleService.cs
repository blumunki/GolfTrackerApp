using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class HoleService : IHoleService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<HoleService> _logger;

        public HoleService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<HoleService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<Hole> AddHoleAsync(Hole hole)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
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
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            // Optional: Add validation for each hole in the list
            _context.Holes.AddRange(holes);
            await _context.SaveChangesAsync();
            return holes.ToList();
        }

        public async Task<bool> DeleteHoleAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            var hole = await _context.Holes.FindAsync(id);
            if (hole == null) return false;

            var hasScores = await _context.Scores.AnyAsync(s => s.HoleId == id);
            if (hasScores)
            {
                _logger.LogWarning("Cannot delete Hole {HoleId}: has linked scores", id);
                throw new InvalidOperationException("Cannot delete this hole because it has recorded scores.");
            }

            _context.Holes.Remove(hole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Hole?> GetHoleByIdAsync(int id)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Holes.Include(h => h.GolfCourse).FirstOrDefaultAsync(h => h.HoleId == id);
        }

        public async Task<List<Hole>> GetHolesForCourseAsync(int golfCourseId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
            return await _context.Holes
                                 .Where(h => h.GolfCourseId == golfCourseId)
                                 .Include(h => h.HoleTees)
                                 .OrderBy(h => h.HoleNumber)
                                 .ToListAsync();
        }

        public async Task<Hole?> UpdateHoleAsync(Hole hole)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            
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