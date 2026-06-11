using GolfTrackerApp.Core.Models;
using GolfTrackerApp.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Tests;

public sealed class HandicapModelTests : IDisposable
{
    private readonly SqliteTestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task HandicapRecord_RoundTripsAndAppearsInPlayerHistory()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);

        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.HandicapRecords.Add(new HandicapRecord
            {
                PlayerId = player.PlayerId,
                HandicapIndex = 18.4m,
                Source = HandicapSource.Personal,
                EffectiveDate = new DateTime(2026, 6, 1),
                CalculationDetails = """{"differentials":[18.2,19.1,20.0]}""",
            });
            await context.SaveChangesAsync();
        }

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var loaded = await context.Players
                .Include(p => p.HandicapRecords)
                .SingleAsync(p => p.PlayerId == player.PlayerId);

            var record = Assert.Single(loaded.HandicapRecords);
            Assert.Equal(18.4m, record.HandicapIndex);
            Assert.Equal(HandicapSource.Personal, record.Source);
            Assert.False(record.IsManualEntry);
            Assert.Null(record.GolfClubId);
            Assert.Null(record.GolfSocietyId);
        }
    }

    [Fact]
    public async Task ScoringDifferential_RoundTripsWithSnapshotInputs()
    {
        var (round, teeSetId, playerId) = await SeedRoundWithTeeSetAsync();

        await using (var context = await _factory.CreateDbContextAsync())
        {
            context.ScoringDifferentials.Add(new ScoringDifferential
            {
                PlayerId = playerId,
                RoundId = round.RoundId,
                TeeSetId = teeSetId,
                AdjustedGrossScore = 90,
                CourseRating = 70.0m,
                SlopeRating = 120,
                Differential = 18.8m,
                IsUsedInCalculation = true,
            });
            await context.SaveChangesAsync();
        }

        await using (var context = await _factory.CreateDbContextAsync())
        {
            var loaded = await context.ScoringDifferentials
                .Include(sd => sd.Round)
                .Include(sd => sd.TeeSet)
                .SingleAsync();

            Assert.Equal(round.RoundId, loaded.Round!.RoundId);
            Assert.Equal(teeSetId, loaded.TeeSet!.TeeSetId);
            Assert.Equal(90, loaded.AdjustedGrossScore);
            Assert.Equal(18.8m, loaded.Differential);
            Assert.True(loaded.IsUsedInCalculation);
        }
    }

    [Fact]
    public async Task ScoringDifferential_SecondRowForSameRoundAndPlayer_IsRejected()
    {
        var (round, teeSetId, playerId) = await SeedRoundWithTeeSetAsync();

        await using var context = await _factory.CreateDbContextAsync();
        context.ScoringDifferentials.AddRange(
            NewDifferential(playerId, round.RoundId, teeSetId),
            NewDifferential(playerId, round.RoundId, teeSetId));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<(Round Round, int TeeSetId, int PlayerId)> SeedRoundWithTeeSetAsync()
    {
        await TestDataBuilder.SeedUserAsync(_factory);
        var player = await TestDataBuilder.SeedPlayerAsync(_factory);
        var course = await TestDataBuilder.SeedCourseAsync(_factory);
        var round = await TestDataBuilder.SeedCompletedRoundAsync(
            _factory, course.GolfCourseId, player.PlayerId);

        await using var context = await _factory.CreateDbContextAsync();
        var teeSetId = await context.TeeSets
            .Where(ts => ts.GolfCourseId == course.GolfCourseId)
            .Select(ts => ts.TeeSetId)
            .SingleAsync();

        return (round, teeSetId, player.PlayerId);
    }

    private static ScoringDifferential NewDifferential(int playerId, int roundId, int teeSetId) => new()
    {
        PlayerId = playerId,
        RoundId = roundId,
        TeeSetId = teeSetId,
        AdjustedGrossScore = 90,
        CourseRating = 70.0m,
        SlopeRating = 120,
        Differential = 18.8m,
    };
}
