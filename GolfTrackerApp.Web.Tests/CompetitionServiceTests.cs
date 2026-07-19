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

    [Fact]
    public async Task List_LoadsEntries_ForSummaryCounts()
    {
        var (user, _) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var competition = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        await _service.AddEntryAsync(competition.CompetitionId, player.PlayerId);

        var listed = Assert.Single(await _service.GetCompetitionsAsync());

        Assert.Single(listed.Entries);
        Assert.Equal(player.PlayerId, listed.Entries.Single().PlayerId);
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

    // --- Host-manager check ---

    [Fact]
    public async Task IsHostManager_RecognizesSocietyManagersAndGrantedClubAdmins()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var clubMember = await TestDataBuilder.SeedUserAsync(_factory, "club-member");
        var society = await SeedSocietyAsync(user.Id);
        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.SocietyMemberships.Add(new SocietyMembership
            {
                GolfSocietyId = society.GolfSocietyId, UserId = user.Id, Role = MembershipRole.Owner,
            });
            context.ClubMemberships.AddRange(
                new ClubMembership
                {
                    GolfClubId = course.GolfClubId, UserId = user.Id, Role = MembershipRole.Admin,
                },
                new ClubMembership
                {
                    GolfClubId = course.GolfClubId, UserId = clubMember.Id, Role = MembershipRole.Member,
                });
            await context.SaveChangesAsync();
        }

        Assert.True(await _service.IsHostManagerAsync(null, society.GolfSocietyId, user.Id));
        Assert.False(await _service.IsHostManagerAsync(null, society.GolfSocietyId, "someone-else"));
        Assert.True(await _service.IsHostManagerAsync(course.GolfClubId, null, user.Id));
        Assert.False(await _service.IsHostManagerAsync(course.GolfClubId, null, clubMember.Id));
        Assert.False(await _service.IsHostManagerAsync(null, null, user.Id));
    }

    [Fact]
    public async Task GetCompetitionsForPlayer_ReturnsEnteredNonTerminalOnly()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 10.0);
        var other = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: "Other");

        var entered = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        var notEntered = await _service.CreateCompetitionAsync(NewCompetition(user.Id, name: "Not entered"));
        var completed = await _service.CreateCompetitionAsync(NewCompetition(user.Id, name: "Done comp"));
        await _service.AddEntryAsync(entered.CompetitionId, player.PlayerId);
        await _service.AddEntryAsync(notEntered.CompetitionId, other.PlayerId);
        await _service.AddEntryAsync(completed.CompetitionId, player.PlayerId);
        await _service.SetStatusAsync(completed.CompetitionId, CompetitionStatus.Completed);

        var forPlayer = await _service.GetCompetitionsForPlayerAsync(player.PlayerId);

        var only = Assert.Single(forPlayer);
        Assert.Equal(entered.CompetitionId, only.CompetitionId);
    }

    [Fact]
    public async Task AssignRound_AutoEntersRoundPlayers_WithHandicapSnapshot()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 18.4);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, teeSetId: teeSetId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-1));

        // No prior entry — assigning the round must create one.
        await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, isUserAdmin: true);

        await using var context = await _factory.CreateDbContextAsync();
        var entry = await context.CompetitionEntries.SingleAsync(e => e.CompetitionId == comp.CompetitionId);
        Assert.Equal(player.PlayerId, entry.PlayerId);
        Assert.Equal(18.4m, entry.HandicapAtEntry);
        Assert.Equal(teeSetId, entry.TeeSetId);

        // Re-assigning is idempotent — still exactly one entry.
        await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, true);
        Assert.Equal(1, await context.CompetitionEntries.CountAsync(e => e.CompetitionId == comp.CompetitionId));
    }

    [Fact]
    public async Task AssignRound_ToCompletedCompetition_StillEnters_ForBackfill()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 12.0);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        await _service.SetStatusAsync(comp.CompetitionId, CompetitionStatus.Completed);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, datePlayed: DateTime.UtcNow.Date.AddDays(-1));

        await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, true);

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(1, await context.CompetitionEntries.CountAsync(e => e.CompetitionId == comp.CompetitionId));
    }

    [Fact]
    public async Task DeleteCompetition_UnlinksRounds_RemovesEntries_KeepsRoundsIntact()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 10.0);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id));
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId, datePlayed: DateTime.UtcNow.Date.AddDays(-1));
        await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, true);

        Assert.True(await _service.DeleteCompetitionAsync(comp.CompetitionId));
        Assert.False(await _service.DeleteCompetitionAsync(comp.CompetitionId)); // already gone

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(0, await context.Competitions.CountAsync());
        Assert.Equal(0, await context.CompetitionEntries.CountAsync());
        var survivingRound = await context.Rounds.SingleAsync(r => r.RoundId == round.RoundId);
        Assert.Null(survivingRound.CompetitionId);
        Assert.Equal(RoundCompletionStatus.Completed, survivingRound.Status);
        Assert.Equal(18, await context.Scores.CountAsync(s => s.RoundId == round.RoundId)); // scores untouched
    }

    // --- Results ---

    [Fact]
    public async Task ComputeResults_MedalRanksByNet_UsingHandicapAtEntry()
    {
        var (user, course) = await SeedUserAndCourseAsync(); // par 4x18, CR 70.0, slope 120
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);
        var scratch = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: "Scratch", handicap: 0.0);
        var highHcp = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: "High", handicap: 20.0);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id, courseId: course.GolfCourseId));
        await _service.AddEntryAsync(comp.CompetitionId, scratch.PlayerId, teeSetId);
        await _service.AddEntryAsync(comp.CompetitionId, highHcp.PlayerId, teeSetId);

        // Scratch shoots 80 (net 79 off CH −1... CR 70 par 72 slope 120: CH(0)= −2 → net 82? compute below);
        // High shoots 90 with CH 19 → net 71: high handicapper wins on net.
        var scratchRound = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, scratch.PlayerId, strokesPerHole: 5, teeSetId: teeSetId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-1)); // gross 90
        var highRound = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, highHcp.PlayerId, strokesPerHole: 5, teeSetId: teeSetId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-1)); // gross 90
        await _service.AssignRoundAsync(scratchRound.RoundId, comp.CompetitionId, user.Id, true);
        await _service.AssignRoundAsync(highRound.RoundId, comp.CompetitionId, user.Id, true);

        var results = await _service.ComputeResultsAsync(comp.CompetitionId);

        // Same gross; the higher handicap gives the lower net → High is 1st.
        var first = results.First();
        Assert.Equal(highHcp.PlayerId, first.PlayerId);
        Assert.Equal(1, first.Position);
        Assert.Equal(90, first.GrossScore);
        // CH(20.0) = 20 × 120/113 + (70.0 − 72) = 21.24 − 2 = 19.24 → 19; net 71.
        Assert.Equal(71, first.NetScore);
        var second = results[1];
        Assert.Equal(scratch.PlayerId, second.PlayerId);
        Assert.Equal(2, second.Position);
        // CH(0.0) = 0 × … + (70 − 72) = −2; net 92.
        Assert.Equal(92, second.NetScore);
    }

    [Fact]
    public async Task ComputeResults_StablefordRanksByPointsDesc_TiesSharePosition()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);
        var comp = await _service.CreateCompetitionAsync(new Competition
        {
            Name = "Stableford Open", GolfCourseId = course.GolfCourseId,
            ScoringFormat = ScoringFormat.Stableford, Date = new DateTime(2026, 7, 1),
            CreatedByUserId = user.Id,
        });

        var players = new List<Player>();
        foreach (var (name, strokes) in new[] { ("A", 4), ("B", 5), ("C", 5) })
        {
            var player = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: name, handicap: 0.0);
            players.Add(player);
            await _service.AddEntryAsync(comp.CompetitionId, player.PlayerId, teeSetId);
            var round = await TestDataBuilder.SeedCompletedRoundAsync(
                _factory, course.GolfCourseId, player.PlayerId, strokesPerHole: strokes, teeSetId: teeSetId,
                datePlayed: DateTime.UtcNow.Date.AddDays(-1));
            await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, true);
        }

        var results = await _service.ComputeResultsAsync(comp.CompetitionId);

        // CH(0) = −2 → one stroke LOST on SI 17+18... v1 StrokesReceivedOnHole gives 0 for CH ≤ 0,
        // so all play net = gross. A: par golf → 2 pts/hole = 36. B, C: bogey → 1 pt/hole = 18.
        Assert.Equal(players[0].PlayerId, results[0].PlayerId);
        Assert.Equal(36, results[0].StablefordPoints);
        Assert.Equal(1, results[0].Position);
        Assert.Equal(2, results[1].Position);
        Assert.Equal(2, results[2].Position); // tie shares 2nd
    }

    [Fact]
    public async Task ComputeResults_EntryWithoutLinkedRound_IsUnranked_AndRecomputeIsIdempotent()
    {
        var (user, course) = await SeedUserAndCourseAsync();
        var teeSetId = await TeeSetIdAsync(course.GolfCourseId);
        var playing = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: "Playing", handicap: 10.0);
        var noShow = await TestDataBuilder.SeedPlayerAsync(_factory, firstName: "NoShow", handicap: 12.0);
        var comp = await _service.CreateCompetitionAsync(NewCompetition(user.Id, courseId: course.GolfCourseId));
        await _service.AddEntryAsync(comp.CompetitionId, playing.PlayerId, teeSetId);
        await _service.AddEntryAsync(comp.CompetitionId, noShow.PlayerId, teeSetId);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, playing.PlayerId, strokesPerHole: 5, teeSetId: teeSetId,
            datePlayed: DateTime.UtcNow.Date.AddDays(-1));
        await _service.AssignRoundAsync(round.RoundId, comp.CompetitionId, user.Id, true);

        var first = await _service.ComputeResultsAsync(comp.CompetitionId);
        var second = await _service.ComputeResultsAsync(comp.CompetitionId);

        foreach (var results in new[] { first, second })
        {
            Assert.Equal(2, results.Count);
            Assert.Equal(playing.PlayerId, results[0].PlayerId);
            Assert.Equal(1, results[0].Position);
            var unranked = results[1];
            Assert.Equal(noShow.PlayerId, unranked.PlayerId);
            Assert.Null(unranked.GrossScore);
            Assert.Null(unranked.Position);
        }
    }

    private async Task<int> TeeSetIdAsync(int courseId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.TeeSets
            .Where(ts => ts.GolfCourseId == courseId)
            .Select(ts => ts.TeeSetId)
            .SingleAsync();
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
