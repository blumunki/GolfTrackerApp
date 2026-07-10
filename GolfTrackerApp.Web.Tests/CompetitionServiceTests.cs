using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class CompetitionServiceTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly CompetitionService _service;

    public CompetitionServiceTests()
    {
        _service = new CompetitionService(_factory, NullLogger<CompetitionService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    // --- Create ---

    [Fact]
    public async Task Create_PersistsWithUpcomingStatus()
    {
        var (user, course) = await SeedUserAndCourseAsync();

        var created = await _service.CreateCompetitionAsync(new Competition
        {
            Name = "Monthly Medal",
            GolfClubId = course.GolfClubId,
            GolfCourseId = course.GolfCourseId,
            ScoringFormat = ScoringFormat.Medal,
            Date = new DateTime(2026, 7, 1),
            CreatedByUserId = user.Id,
        });

        var loaded = await _service.GetCompetitionByIdAsync(created.CompetitionId);
        Assert.NotNull(loaded);
        Assert.Equal("Monthly Medal", loaded!.Name);
        Assert.Equal(CompetitionStatus.Upcoming, loaded.Status);
        Assert.Equal(course.GolfClubId, loaded.GolfClubId);
        Assert.NotNull(loaded.GolfCourse);
    }

    [Fact]
    public async Task Create_RejectsInvalidInput()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var society = await SeedSocietyAsync(user.Id);

        // No name.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCompetitionAsync(
            NewCompetition(user.Id, name: " ")));
        // No date.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCompetitionAsync(
            new Competition { Name = "X", CreatedByUserId = user.Id }));
        // Both hosts.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCompetitionAsync(
            NewCompetition(user.Id, clubId: course.GolfClubId, societyId: society.GolfSocietyId)));
        // Unknown course.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCompetitionAsync(
            NewCompetition(user.Id, courseId: 9999)));
        // No creator.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateCompetitionAsync(
            NewCompetition(createdByUserId: "")));
    }

    // --- List ---

    [Fact]
    public async Task List_FiltersByHostAndStatus()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var society = await SeedSocietyAsync(user.Id);

        var clubComp = await _service.CreateCompetitionAsync(NewCompetition(user.Id, clubId: course.GolfClubId));
        var societyComp = await _service.CreateCompetitionAsync(NewCompetition(user.Id, societyId: society.GolfSocietyId));
        await _service.SetStatusAsync(societyComp.CompetitionId, CompetitionStatus.InProgress);

        Assert.Single(await _service.GetCompetitionsAsync(golfClubId: course.GolfClubId));
        Assert.Single(await _service.GetCompetitionsAsync(golfSocietyId: society.GolfSocietyId));
        var inProgress = await _service.GetCompetitionsAsync(status: CompetitionStatus.InProgress);
        Assert.Equal(societyComp.CompetitionId, Assert.Single(inProgress).CompetitionId);
        Assert.Equal(2, (await _service.GetCompetitionsAsync()).Count);
        _ = clubComp;
    }

    // --- Status ---

    [Fact]
    public async Task SetStatus_TerminalStatesCannotBeReopened()
    {
        var (user, _) = await SeedUserAndCourseAsync();
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));

        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.InProgress);
        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.Completed);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.InProgress));
        Assert.Null(await _service.SetStatusAsync(9999, CompetitionStatus.InProgress));
    }

    [Fact]
    public async Task Update_ChangesDetails_ButNotWhenTerminal()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));

        comp.Name = "Renamed Stableford";
        comp.ScoringFormat = ScoringFormat.Stableford;
        comp.GolfCourseId = course.GolfCourseId;
        var updated = await _service.UpdateCompetitionAsync(comp);
        Assert.Equal("Renamed Stableford", updated!.Name);
        Assert.Equal(ScoringFormat.Stableford, updated.ScoringFormat);

        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.Cancelled);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateCompetitionAsync(comp));
    }

    // --- Entries ---

    [Fact]
    public async Task AddEntry_SnapshotsHandicap_AndRejectsDuplicates()
    {
        var (user, _) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 18.4);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));

        var entry = await _service.AddEntryAsync(comp.CompetitionId, player.PlayerId);

        Assert.Equal(18.4m, entry.HandicapAtEntry);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddEntryAsync(comp.CompetitionId, player.PlayerId));

        // Unknown player / competition / tee set.
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddEntryAsync(comp.CompetitionId, 9999));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddEntryAsync(9999, player.PlayerId));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddEntryAsync(comp.CompetitionId, player.PlayerId, teeSetId: 9999));
    }

    [Fact]
    public async Task RemoveEntry_Withdraws_ButNotFromCompletedCompetition()
    {
        var (user, _) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        await _service.AddEntryAsync(comp.CompetitionId, player.PlayerId);

        Assert.True(await _service.RemoveEntryAsync(comp.CompetitionId, player.PlayerId));
        Assert.False(await _service.RemoveEntryAsync(comp.CompetitionId, player.PlayerId));

        await _service.AddEntryAsync(comp.CompetitionId, player.PlayerId);
        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.InProgress);
        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.Completed);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveEntryAsync(comp.CompetitionId, player.PlayerId));
    }

    // --- Round linking ---

    [Fact]
    public async Task AssignRound_LinksOwnRound_AndPreservesRoundType()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));

        var linked = await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, isUserAdmin: false);

        Assert.Equal(comp.CompetitionId, linked!.CompetitionId);
        Assert.Equal(round.RoundType, linked.RoundType); // Friendly/Competitive untouched

        var unlinked = await _service.UnassignRoundAsync(round.RoundId, user.Id, isUserAdmin: false);
        Assert.Null(unlinked!.CompetitionId);
    }

    [Fact]
    public async Task AssignRound_RejectsOtherUsersRounds_UnlessAdmin()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(_factory, course.GolfCourseId, player.PlayerId);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, "someone-else", isUserAdmin: false));

        var asAdmin = await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, "someone-else", isUserAdmin: true);
        Assert.Equal(comp.CompetitionId, asAdmin!.CompetitionId);

        // Cancelled competitions accept no rounds.
        await _service.UnassignRoundAsync(round.RoundId, user.Id, false);
        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.Cancelled);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, false));

        Assert.Null(await _service.AssignRoundAsync(9999, comp.CompetitionId, user.Id, false));
    }

    // --- helpers ---

    private async Task<(Core.Data.ApplicationUser User, GolfCourse Course)> SeedUserAndCourseAsync()
    {
        var user = await TestDataBuilder.SeedUserAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        return (user, course);
    }

    private async Task<GolfSociety> SeedSocietyAsync(string userId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var society = new GolfSociety { Name = "Test Society", CreatedByUserId = userId };
        context.GolfSocieties.Add(society);
        await context.SaveChangesAsync();
        return society;
    }

    private static Competition NewCompetition(
        string createdByUserId, string name = "Test Competition",
        int? clubId = null, int? societyId = null, int? courseId = null) => new()
    {
        Name = name,
        GolfClubId = clubId,
        GolfSocietyId = societyId,
        GolfCourseId = courseId,
        ScoringFormat = ScoringFormat.Medal,
        Date = new DateTime(2026, 7, 1),
        CreatedByUserId = createdByUserId,
    };
}
