using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Core.Services;

public class ClubMembershipService : IClubMembershipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ClubMembershipService> _logger;

    public ClubMembershipService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ClubMembershipService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<ClubMembership>> GetMembershipsForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ClubMemberships
            .Where(cm => cm.UserId == userId)
            .Include(cm => cm.GolfClub)
            .OrderBy(cm => cm.GolfClub!.Name)
            .ToListAsync();
    }

    public async Task<List<ClubMembership>> GetMembershipsForClubAsync(int golfClubId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ClubMemberships
            .Where(cm => cm.GolfClubId == golfClubId)
            .Include(cm => cm.User)
            .OrderBy(cm => cm.JoinedAt)
            .ToListAsync();
    }

    public async Task<ClubMembership?> GetMembershipAsync(int golfClubId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ClubMemberships
            .Include(cm => cm.GolfClub)
            .FirstOrDefaultAsync(cm => cm.GolfClubId == golfClubId && cm.UserId == userId);
    }

    public async Task<ClubMembership> JoinClubAsync(int golfClubId, string userId, string? membershipNumber = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var existing = await context.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.GolfClubId == golfClubId && cm.UserId == userId);
        
        if (existing != null)
            throw new InvalidOperationException("User is already a member of this club.");

        var membership = new ClubMembership
        {
            GolfClubId = golfClubId,
            UserId = userId,
            Role = MembershipRole.Member,
            MembershipNumber = membershipNumber,
            JoinedAt = DateTime.UtcNow
        };

        context.ClubMemberships.Add(membership);
        await context.SaveChangesAsync();
        return membership;
    }

    public async Task<bool> LeaveClubAsync(int golfClubId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var membership = await context.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.GolfClubId == golfClubId && cm.UserId == userId);
        
        if (membership == null) return false;

        context.ClubMemberships.Remove(membership);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ClubMembership?> UpdateMembershipAsync(int golfClubId, string userId, MembershipRole role, string? membershipNumber)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var membership = await context.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.GolfClubId == golfClubId && cm.UserId == userId);
        
        if (membership == null) return null;

        membership.Role = role;
        membership.MembershipNumber = membershipNumber;
        await context.SaveChangesAsync();
        return membership;
    }

    public async Task<ClubMembership?> SetClubAdminAsync(int golfClubId, string userId, bool isAdmin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (!await context.GolfClubs.AnyAsync(c => c.GolfClubId == golfClubId))
            throw new ArgumentException($"Golf club with ID {golfClubId} does not exist.", nameof(golfClubId));
        if (!await context.Users.AnyAsync(u => u.Id == userId))
            throw new ArgumentException($"User with ID '{userId}' does not exist.", nameof(userId));

        var membership = await context.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.GolfClubId == golfClubId && cm.UserId == userId);

        if (membership is null)
        {
            if (!isAdmin) return null;

            membership = new ClubMembership
            {
                GolfClubId = golfClubId,
                UserId = userId,
                Role = MembershipRole.Admin,
                JoinedAt = DateTime.UtcNow,
            };
            context.ClubMemberships.Add(membership);
        }
        else
        {
            membership.Role = isAdmin ? MembershipRole.Admin : MembershipRole.Member;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation(
            "Set club {GolfClubId} admin status for user {UserId} to {IsAdmin}",
            golfClubId,
            userId,
            isAdmin);
        return membership;
    }
}
