using GolfTrackerApp.Web.Models;

namespace GolfTrackerApp.Web.Services;

public interface IClubMembershipService
{
    Task<List<ClubMembership>> GetMembershipsForUserAsync(string userId);
    Task<List<ClubMembership>> GetMembershipsForClubAsync(int golfClubId);
    Task<ClubMembership?> GetMembershipAsync(int golfClubId, string userId);
    Task<ClubMembership> JoinClubAsync(int golfClubId, string userId, string? membershipNumber = null);
    Task<bool> LeaveClubAsync(int golfClubId, string userId);
    Task<ClubMembership?> UpdateMembershipAsync(int golfClubId, string userId, MembershipRole role, string? membershipNumber);
}
