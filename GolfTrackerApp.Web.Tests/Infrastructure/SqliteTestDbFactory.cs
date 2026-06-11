using GolfTrackerApp.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GolfTrackerApp.Web.Tests.Infrastructure;

/// <summary>
/// IDbContextFactory backed by a single in-memory SQLite database.
/// The database lives as long as the underlying connection stays open,
/// so every context created by this factory sees the same data.
/// Dispose the factory to drop the database.
/// </summary>
public sealed class SqliteTestDbFactory : IDbContextFactory<ApplicationDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public SqliteTestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateDbContext() => new(_options);

    public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
