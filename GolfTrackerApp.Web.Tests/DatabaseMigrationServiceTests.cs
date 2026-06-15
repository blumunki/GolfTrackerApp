using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GolfTrackerApp.Web.Tests;

public sealed class DatabaseMigrationServiceTests : IDisposable
{
    // Migrations are attributed to the derived context types, so this factory yields
    // SqliteApplicationDbContext over a single shared in-memory connection (NOT EnsureCreated)
    // so the real migration chain can be applied and inspected.
    private sealed class MigratableSqliteFactory : IDbContextFactory<ApplicationDbContext>, IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SqliteApplicationDbContext> _options;

        public MigratableSqliteFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<SqliteApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;
        }

        public ApplicationDbContext CreateDbContext() => new SqliteApplicationDbContext(_options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ApplicationDbContext>(new SqliteApplicationDbContext(_options));

        public void Dispose() => _connection.Dispose();
    }

    private readonly MigratableSqliteFactory _factory = new();
    private readonly DatabaseMigrationService _service;

    public DatabaseMigrationServiceTests()
    {
        _service = new DatabaseMigrationService(_factory, NullLogger<DatabaseMigrationService>.Instance);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetStatus_BeforeMigrating_ReportsAllMigrationsPending()
    {
        var status = await _service.GetStatusAsync();

        Assert.True(status.CanConnect);
        Assert.NotEmpty(status.All);
        Assert.Empty(status.Applied);
        Assert.Equal(status.All.Count, status.Pending.Count);
        Assert.True(status.HasPending);
    }

    [Fact]
    public async Task ApplyPending_AppliesEverything_ThenStatusIsClean()
    {
        var applied = await _service.ApplyPendingMigrationsAsync();
        Assert.True(applied > 0);

        var status = await _service.GetStatusAsync();
        Assert.True(status.CanConnect);
        Assert.NotEmpty(status.Applied);
        Assert.Empty(status.Pending);
        Assert.False(status.HasPending);
    }

    [Fact]
    public async Task ApplyPending_WhenUpToDate_IsNoOp()
    {
        await _service.ApplyPendingMigrationsAsync();

        var second = await _service.ApplyPendingMigrationsAsync();

        Assert.Equal(0, second);
    }
}
