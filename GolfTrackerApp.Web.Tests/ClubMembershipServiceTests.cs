using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class ClubMembershipServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly ClubMembershipService _service;

    public ClubMembershipServiceTests()
    {
        _service = new ClubMembershipService(
            _factory,
            NullLogger<ClubMembershipService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SetClubAdmin_GrantCreatesMembership_AndRevokeKeepsMember()
    {
        var user = await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);

        var granted = await _service.SetClubAdminAsync(course.GolfClubId, user.Id, isAdmin: true);

        Assert.NotNull(granted);
        Assert.Equal(MembershipRole.Admin, granted!.Role);

        var revoked = await _service.SetClubAdminAsync(course.GolfClubId, user.Id, isAdmin: false);

        Assert.NotNull(revoked);
        Assert.Equal(granted.ClubMembershipId, revoked!.ClubMembershipId);
        Assert.Equal(MembershipRole.Member, revoked.Role);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(1, await context.ClubMemberships.CountAsync());
    }

    [Fact]
    public async Task SetClubAdmin_PromotesExistingMemberWithoutChangingMembershipDetails()
    {
        var user = await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var membership = await _service.JoinClubAsync(course.GolfClubId, user.Id, "ABC-123");

        var granted = await _service.SetClubAdminAsync(course.GolfClubId, user.Id, isAdmin: true);

        Assert.NotNull(granted);
        Assert.Equal(membership.ClubMembershipId, granted!.ClubMembershipId);
        Assert.Equal("ABC-123", granted.MembershipNumber);
        Assert.Equal(membership.JoinedAt, granted.JoinedAt);
        Assert.Equal(MembershipRole.Admin, granted.Role);
    }

    [Fact]
    public async Task SetClubAdmin_RevokeWithoutMembership_IsIdempotent()
    {
        var user = await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);

        var membership = await _service.SetClubAdminAsync(course.GolfClubId, user.Id, isAdmin: false);

        Assert.Null(membership);
    }

    [Fact]
    public async Task SetClubAdmin_RejectsUnknownClubOrUser()
    {
        var user = await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SetClubAdminAsync(9999, user.Id, isAdmin: true));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SetClubAdminAsync(course.GolfClubId, "missing-user", isAdmin: true));
    }
}
