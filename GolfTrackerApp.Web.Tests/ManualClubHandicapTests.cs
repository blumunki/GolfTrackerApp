using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Core.Services;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class ManualClubHandicapTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();
    private readonly HandicapService _service;

    public ManualClubHandicapTests()
    {
        _service = new HandicapService(_factory, NullLogger<HandicapService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Add_ForcesClubRegionalManualEntry_AndPersists()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();

        var added = await _service.AddManualClubHandicapAsync(new HandicapRecord
        {
            PlayerId = player.PlayerId,
            GolfClubId = clubId,
            HandicapIndex = 12.3m,
            EffectiveDate = new DateTime(2026, 5, 1),
            // deliberately wrong — the service must force these:
            Source = HandicapSource.Personal,
            IsManualEntry = false,
            GolfSocietyId = 99,
        });

        await using var context = await _factory.CreateDbContextAsync();
        var persisted = await context.HandicapRecords.SingleAsync(h => h.HandicapRecordId == added.HandicapRecordId);
        Assert.Equal(HandicapSource.ClubRegional, persisted.Source);
        Assert.True(persisted.IsManualEntry);
        Assert.Null(persisted.GolfSocietyId);
        Assert.Equal(12.3m, persisted.HandicapIndex);
    }

    [Theory]
    [InlineData(null, 12.3, "2026-05-01")] // no club
    [InlineData(1, 55.0, "2026-05-01")]    // index above 54.0
    [InlineData(1, -10.5, "2026-05-01")]   // implausible plus handicap
    [InlineData(1, 12.3, null)]            // no effective date
    public async Task Add_RejectsInvalidEntries(int? clubId, double index, string? effectiveDate)
    {
        var (player, seededClubId) = await SeedPlayerAndClubAsync();

        var record = new HandicapRecord
        {
            PlayerId = player.PlayerId,
            GolfClubId = clubId is null ? null : seededClubId,
            HandicapIndex = (decimal)index,
            EffectiveDate = effectiveDate is null ? default : DateTime.Parse(effectiveDate),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddManualClubHandicapAsync(record));
    }

    [Fact]
    public async Task Add_RejectsUnknownPlayerAndClub()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddManualClubHandicapAsync(
            NewEntry(playerId: 9999, clubId)));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddManualClubHandicapAsync(
            NewEntry(player.PlayerId, clubId: 9999)));
    }

    [Fact]
    public async Task Update_ChangesValues_AndReturnsNullForUnknownId()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        var added = await _service.AddManualClubHandicapAsync(NewEntry(player.PlayerId, clubId));

        added.HandicapIndex = 9.8m;
        added.ExpiryDate = new DateTime(2027, 5, 1);
        var updated = await _service.UpdateManualClubHandicapAsync(added);
        Assert.NotNull(updated);
        Assert.Equal(9.8m, updated!.HandicapIndex);

        var unknown = NewEntry(player.PlayerId, clubId);
        unknown.HandicapRecordId = 9999;
        Assert.Null(await _service.UpdateManualClubHandicapAsync(unknown));
    }

    [Fact]
    public async Task UpdateAndDelete_RejectCalculatedRecords()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        int calculatedId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            var calculated = new HandicapRecord
            {
                PlayerId = player.PlayerId,
                HandicapIndex = 16.8m,
                Source = HandicapSource.Personal,
                EffectiveDate = new DateTime(2026, 5, 1),
                IsManualEntry = false,
            };
            context.HandicapRecords.Add(calculated);
            await context.SaveChangesAsync();
            calculatedId = calculated.HandicapRecordId;
        }

        var update = NewEntry(player.PlayerId, clubId);
        update.HandicapRecordId = calculatedId;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateManualClubHandicapAsync(update));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteManualClubHandicapAsync(calculatedId));
    }

    [Fact]
    public async Task Delete_RemovesEntry_AndReturnsFalseForUnknownId()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        var added = await _service.AddManualClubHandicapAsync(NewEntry(player.PlayerId, clubId));

        Assert.True(await _service.DeleteManualClubHandicapAsync(added.HandicapRecordId));
        Assert.False(await _service.DeleteManualClubHandicapAsync(added.HandicapRecordId));

        await using var context = await _factory.CreateDbContextAsync();
        Assert.Equal(0, await context.HandicapRecords.CountAsync());
    }

    [Fact]
    public async Task GetActiveHandicaps_ReturnsLatestPerSourceContext()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        await _service.AddManualClubHandicapAsync(
            NewEntry(player.PlayerId, clubId, index: 14.0m, effective: new DateTime(2026, 1, 1)));
        await _service.AddManualClubHandicapAsync(
            NewEntry(player.PlayerId, clubId, index: 12.5m, effective: new DateTime(2026, 5, 1)));
        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.HandicapRecords.Add(new HandicapRecord
            {
                PlayerId = player.PlayerId,
                HandicapIndex = 16.8m,
                Source = HandicapSource.Personal,
                EffectiveDate = new DateTime(2026, 4, 1),
            });
            await context.SaveChangesAsync();
        }

        var active = await _service.GetActiveHandicapsAsync(player.PlayerId);

        Assert.Equal(2, active.Count);
        Assert.Equal(16.8m, active.Single(h => h.Source == HandicapSource.Personal).HandicapIndex);
        Assert.Equal(12.5m, active.Single(h => h.Source == HandicapSource.ClubRegional).HandicapIndex);
    }

    [Fact]
    public async Task DisplayHandicap_TracksLatestClubEntry_WhenPrimarySourceIsClubRegional()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        await using (var context = await _factory.CreateDbContextAsync())
        {
            (await context.Players.FindAsync(player.PlayerId))!.PrimaryHandicapSource = HandicapSource.ClubRegional;
            await context.SaveChangesAsync();
        }

        var added = await _service.AddManualClubHandicapAsync(NewEntry(player.PlayerId, clubId, index: 12.3m));
        Assert.Equal(12.3, await DisplayHandicapAsync(player.PlayerId));

        await _service.DeleteManualClubHandicapAsync(added.HandicapRecordId);
        Assert.Null(await DisplayHandicapAsync(player.PlayerId));
    }

    [Fact]
    public async Task GetRecentDifferentials_ReturnsNewestFirstWithRoundData()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        int teeSetId;
        await using (var context = await _factory.CreateDbContextAsync())
        {
            teeSetId = await context.TeeSets.Select(t => t.TeeSetId).SingleAsync();
        }

        foreach (var (strokes, daysAgo) in new[] { (5, 2), (4, 1) })
        {
            var round = await TestDataBuilder.SeedCompletedRoundAsync(
                _factory, course.GolfCourseId, player.PlayerId,
                datePlayed: DateTime.UtcNow.Date.AddDays(-daysAgo),
                strokesPerHole: strokes, teeSetId: teeSetId);
            await _service.OnRoundCompletedAsync(round.RoundId);
        }

        var differentials = await _service.GetRecentDifferentialsAsync(player.PlayerId);

        Assert.Equal(2, differentials.Count);
        Assert.Equal(new[] { 1.9m, 18.8m }, differentials.Select(d => d.Differential)); // newest first
        Assert.All(differentials, d => Assert.NotNull(d.Round?.GolfCourse));
    }

    [Fact]
    public async Task SetPrimarySource_RefreshesDisplayHandicapFromThatSource()
    {
        var (player, clubId) = await SeedPlayerAndClubAsync();
        await _service.AddManualClubHandicapAsync(NewEntry(player.PlayerId, clubId, index: 12.3m));

        var updated = await _service.SetPrimaryHandicapSourceAsync(player.PlayerId, HandicapSource.ClubRegional);

        Assert.NotNull(updated);
        Assert.Equal(HandicapSource.ClubRegional, updated!.PrimaryHandicapSource);
        Assert.Equal(12.3, updated.Handicap);

        // A source with no records clears the display value.
        updated = await _service.SetPrimaryHandicapSourceAsync(player.PlayerId, HandicapSource.Personal);
        Assert.Null(updated!.Handicap);
    }

    [Fact]
    public async Task SetPrimarySource_Null_KeepsLegacyManualHandicap()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory, handicap: 18.0);

        var updated = await _service.SetPrimaryHandicapSourceAsync(player.PlayerId, null);

        Assert.Null(updated!.PrimaryHandicapSource);
        Assert.Equal(18.0, updated.Handicap); // untouched

        Assert.Null(await _service.SetPrimaryHandicapSourceAsync(9999, HandicapSource.Personal));
    }

    private async Task<(Player Player, int ClubId)> SeedPlayerAndClubAsync()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        return (player, course.GolfClubId);
    }

    private async Task<double?> DisplayHandicapAsync(int playerId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return (await context.Players.SingleAsync(p => p.PlayerId == playerId)).Handicap;
    }

    private static HandicapRecord NewEntry(
        int playerId, int clubId, decimal index = 12.3m, DateTime? effective = null) => new()
    {
        PlayerId = playerId,
        GolfClubId = clubId,
        HandicapIndex = index,
        EffectiveDate = effective ?? new DateTime(2026, 5, 1),
    };
}
