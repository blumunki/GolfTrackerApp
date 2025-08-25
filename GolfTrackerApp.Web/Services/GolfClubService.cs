using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public class GolfClubService : IGolfClubService
    {
        private readonly ApplicationDbContext _context;

        public GolfClubService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GolfClub> AddGolfClubAsync(GolfClub golfClub)
        {
            _context.GolfClubs.Add(golfClub);
            await _context.SaveChangesAsync();
            return golfClub;
        }

        public async Task<bool> DeleteGolfClubAsync(int id)
        {
            var golfClub = await _context.GolfClubs.FindAsync(id);
            if (golfClub == null)
            {
                return false;
            }
            // Consider implications: what if courses are linked?
            // For now, simple delete. Later, you might check for linked courses.
            _context.GolfClubs.Remove(golfClub);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GolfClub>> GetAllGolfClubsAsync()
        {
            return await _context.GolfClubs.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<GolfClub?> GetGolfClubByIdAsync(int id)
        {
            // Include courses if needed when viewing a single club's details
            return await _context.GolfClubs
                                 .Include(c => c.GolfCourses)
                                 .FirstOrDefaultAsync(c => c.GolfClubId == id);
        }

        public async Task<GolfClub?> UpdateGolfClubAsync(GolfClub golfClub)
        {
            var existingClub = await _context.GolfClubs.FindAsync(golfClub.GolfClubId);
            if (existingClub == null)
            {
                return null;
            }
            _context.Entry(existingClub).CurrentValues.SetValues(golfClub);
            await _context.SaveChangesAsync();
            return existingClub;
        }
        public async Task<List<GolfClub>> SearchGolfClubsAsync(string searchTerm)
        {
            var query = _context.GolfClubs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string pattern = $"%{searchTerm}%";
                query = query.Where(c => 
                    EF.Functions.Like(c.Name, pattern) ||
                    (c.City != null && EF.Functions.Like(c.City, pattern)) ||
                    (c.Country != null && EF.Functions.Like(c.Country, pattern))
                );
            }
            return await query.OrderBy(c => c.Name).ToListAsync();
        }
    }
}