// In GolfTrackerApp.Web/Services/IGolfCourseService.cs
using GolfTrackerApp.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Web.Services
{
    public interface IGolfCourseService
    {
        Task<List<GolfCourse>> GetAllGolfCoursesAsync();
        Task<GolfCourse?> GetGolfCourseByIdAsync(int id);
        Task<GolfCourse> AddGolfCourseAsync(GolfCourse golfCourse);
        Task<GolfCourse?> UpdateGolfCourseAsync(GolfCourse golfCourse);
        Task<bool> DeleteGolfCourseAsync(int id);
    }
}