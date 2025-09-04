// In GolfTrackerApp.Web/Services/IHoleService.cs
using GolfTrackerApp.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Shared.Services
{
    public interface IHoleService
    {
        Task<List<Hole>> GetHolesForCourseAsync(int golfCourseId);
        Task<Hole?> GetHoleByIdAsync(int id);
        Task<Hole> AddHoleAsync(Hole hole);
        Task<List<Hole>> AddHolesAsync(IEnumerable<Hole> holes); // For bulk adding
        Task<Hole?> UpdateHoleAsync(Hole hole);
        Task<bool> DeleteHoleAsync(int id);
    }
}