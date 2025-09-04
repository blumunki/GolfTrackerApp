// In GolfTrackerApp.Web/Services/IGolfCourseService.cs
using GolfTrackerApp.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Shared.Services
{
    public interface IGolfCourseService
    {
        Task<List<GolfCourse>> GetAllGolfCoursesAsync();
        Task<List<GolfCourse>> SearchGolfCoursesAsync(string searchTerm);
        Task<GolfCourse?> GetGolfCourseByIdAsync(int id);
        Task<GolfCourse> AddGolfCourseAsync(GolfCourse golfCourse);
        Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse);
        Task<bool> DeleteGolfCourseAsync(int id);
    }
}