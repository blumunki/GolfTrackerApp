// In GolfTrackerApp.Web/Services/IGolfClubService.cs
using GolfTrackerApp.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GolfTrackerApp.Shared.Services
{
    public interface IGolfClubService
    {
        Task<List<GolfClub>> GetAllGolfClubsAsync();
        Task<GolfClub?> GetGolfClubByIdAsync(int id);
        Task<GolfClub> AddGolfClubAsync(GolfClub golfClub);
        Task<GolfClub?> UpdateGolfClubAsync(GolfClub golfClub);
        Task<bool> DeleteGolfClubAsync(int id);
        Task<List<GolfClub>> SearchGolfClubsAsync(string searchTerm);
    }
}