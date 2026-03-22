using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IGolfSocietyService
{
    Task<List<GolfSociety>> GetSocietiesForUserAsync(string userId);
    Task<List<GolfSociety>> GetAllSocietiesAsync();
    Task<GolfSociety?> GetSocietyByIdAsync(int societyId);
    Task<GolfSociety> CreateSocietyAsync(GolfSociety society, string creatorUserId);
    Task<GolfSociety> UpdateSocietyAsync(GolfSociety society);
    Task<bool> DeleteSocietyAsync(int societyId, string userId);
    Task<SocietyMembership> JoinSocietyAsync(int societyId, string userId);
    Task<bool> LeaveSocietyAsync(int societyId, string userId);
    Task<SocietyMembership?> UpdateMemberRoleAsync(int societyId, string userId, MembershipRole role, string requestingUserId);
    Task<bool> IsUserMemberAsync(int societyId, string userId);
    Task<bool> IsUserAdminAsync(int societyId, string userId);
}
