// In GolfTrackerApp.Web/Services/IGolfCourseService.cs
using GolfTrackerApp.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Core.Services
{
    public interface IGolfCourseService
    {
        Task<List<GolfCourse>> GetAllGolfCoursesAsync();
        Task<List<GolfCourse>> SearchGolfCoursesAsync(string searchTerm);
        Task<GolfCourse?> GetGolfCourseByIdAsync(int id);
        Task<List<GolfCourse>> GetCoursesForClubAsync(int clubId);
        Task<GolfCourse> AddGolfCourseAsync(GolfCourse golfCourse);
        Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse);
        Task<bool> DeleteGolfCourseAsync(int id);
    }
}