using GolfTrackerApp.Core.Models;

namespace GolfTrackerApp.Core.Services;

public interface IClubMembershipService
{
    Task<List<ClubMembership>> GetMembershipsForUserAsync(string userId);
    Task<List<ClubMembership>> GetMembershipsForClubAsync(int golfClubId);
    Task<ClubMembership?> GetMembershipAsync(int golfClubId, string userId);
    Task<ClubMembership> JoinClubAsync(int golfClubId, string userId, string? membershipNumber = null);
    Task<bool> LeaveClubAsync(int golfClubId, string userId);
    Task<ClubMembership?> UpdateMembershipAsync(int golfClubId, string userId, MembershipRole role, string? membershipNumber);

    /// <summary>
    /// Grants or revokes club-admin rights. Granting creates a membership when needed;
    /// revoking keeps the user as a club member. The caller must enforce global-admin authorization.
    /// </summary>
    Task<ClubMembership?> SetClubAdminAsync(int golfClubId, string userId, bool isAdmin);
}
