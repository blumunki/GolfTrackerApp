using GolfTrackerApp.Web.Data;
using GolfTrackerApp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Services;

public class GolfSocietyService : IGolfSocietyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<GolfSocietyService> _logger;

    public GolfSocietyService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<GolfSocietyService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<List<GolfSociety>> GetSocietiesForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GolfSocieties
            .Where(gs => gs.IsActive && gs.Memberships.Any(m => m.UserId == userId))
            .Include(gs => gs.Memberships)
            .OrderBy(gs => gs.Name)
            .ToListAsync();
    }

    public async Task<List<GolfSociety>> GetAllSocietiesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GolfSocieties
            .Where(gs => gs.IsActive)
            .Include(gs => gs.Memberships)
            .OrderBy(gs => gs.Name)
            .ToListAsync();
    }

    public async Task<GolfSociety?> GetSocietyByIdAsync(int societyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.GolfSocieties
            .Include(gs => gs.Memberships)
                .ThenInclude(m => m.User)
            .Include(gs => gs.CreatedByUser)
            .FirstOrDefaultAsync(gs => gs.GolfSocietyId == societyId);
    }

    public async Task<GolfSociety> CreateSocietyAsync(GolfSociety society, string creatorUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        society.CreatedByUserId = creatorUserId;
        society.CreatedAt = DateTime.UtcNow;
        society.IsActive = true;

        context.GolfSocieties.Add(society);
        await context.SaveChangesAsync();

        // Auto-add creator as Owner
        context.SocietyMemberships.Add(new SocietyMembership
        {
            GolfSocietyId = society.GolfSocietyId,
            UserId = creatorUserId,
            Role = MembershipRole.Owner,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        return society;
    }

    public async Task<GolfSociety> UpdateSocietyAsync(GolfSociety society)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.GolfSocieties.Update(society);
        await context.SaveChangesAsync();
        return society;
    }

    public async Task<bool> DeleteSocietyAsync(int societyId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var society = await context.GolfSocieties
            .Include(gs => gs.Memberships)
            .FirstOrDefaultAsync(gs => gs.GolfSocietyId == societyId);
        
        if (society == null) return false;

        // Only Owner can delete
        var membership = society.Memberships.FirstOrDefault(m => m.UserId == userId);
        if (membership?.Role != MembershipRole.Owner) return false;

        society.IsActive = false; // Soft delete
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<SocietyMembership> JoinSocietyAsync(int societyId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var existing = await context.SocietyMemberships
            .FirstOrDefaultAsync(m => m.GolfSocietyId == societyId && m.UserId == userId);
        
        if (existing != null)
            throw new InvalidOperationException("User is already a member of this society.");

        var membership = new SocietyMembership
        {
            GolfSocietyId = societyId,
            UserId = userId,
            Role = MembershipRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        context.SocietyMemberships.Add(membership);
        await context.SaveChangesAsync();
        return membership;
    }

    public async Task<bool> LeaveSocietyAsync(int societyId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var membership = await context.SocietyMemberships
            .FirstOrDefaultAsync(m => m.GolfSocietyId == societyId && m.UserId == userId);
        
        if (membership == null) return false;
        if (membership.Role == MembershipRole.Owner)
            throw new InvalidOperationException("Owner cannot leave the society. Transfer ownership first.");

        context.SocietyMemberships.Remove(membership);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<SocietyMembership?> UpdateMemberRoleAsync(int societyId, string userId, MembershipRole role, string requestingUserId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check requester is admin or owner
        var requesterMembership = await context.SocietyMemberships
            .FirstOrDefaultAsync(m => m.GolfSocietyId == societyId && m.UserId == requestingUserId);
        
        if (requesterMembership == null || requesterMembership.Role == MembershipRole.Member)
            return null;

        // Only Owner can promote to Admin/Owner
        if (role >= MembershipRole.Admin && requesterMembership.Role != MembershipRole.Owner)
            return null;

        var membership = await context.SocietyMemberships
            .FirstOrDefaultAsync(m => m.GolfSocietyId == societyId && m.UserId == userId);
        
        if (membership == null) return null;

        membership.Role = role;
        await context.SaveChangesAsync();
        return membership;
    }

    public async Task<bool> IsUserMemberAsync(int societyId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SocietyMemberships
            .AnyAsync(m => m.GolfSocietyId == societyId && m.UserId == userId);
    }

    public async Task<bool> IsUserAdminAsync(int societyId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SocietyMemberships
            .AnyAsync(m => m.GolfSocietyId == societyId && m.UserId == userId && m.Role >= MembershipRole.Admin);
    }
}
