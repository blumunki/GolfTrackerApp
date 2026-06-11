using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GolfTrackerApp.Web.Data;

// Each database provider has its own migration set. EF Core discovers every
// migration attributed to a context type within the migrations assembly, so the
// two sets must be attached to two distinct context types:
//
//   SqliteApplicationDbContext    → Data/Migrations/Sqlite/    (development)
//   SqlServerApplicationDbContext → Data/Migrations/SqlServer/ (production)
//
// Adding a schema change requires BOTH:
//   dotnet ef migrations add <Name> --context SqliteApplicationDbContext --output-dir Data/Migrations/Sqlite
//   dotnet ef migrations add <Name> --context SqlServerApplicationDbContext --output-dir Data/Migrations/SqlServer
//
// The derived types add no model or behaviour — application code keeps using
// ApplicationDbContext via the forwarding registrations in Program.cs.

public sealed class SqliteApplicationDbContext : ApplicationDbContext
{
    public SqliteApplicationDbContext(DbContextOptions<SqliteApplicationDbContext> options)
        : base(options)
    {
    }
}

public sealed class SqlServerApplicationDbContext : ApplicationDbContext
{
    public SqlServerApplicationDbContext(DbContextOptions<SqlServerApplicationDbContext> options)
        : base(options)
    {
    }
}

/// <summary>
/// Adapts the provider-specific factory registered in Program.cs to the
/// IDbContextFactory&lt;ApplicationDbContext&gt; that all services depend on.
/// </summary>
public sealed class DerivedDbContextFactory<TDerived> : IDbContextFactory<ApplicationDbContext>
    where TDerived : ApplicationDbContext
{
    private readonly IDbContextFactory<TDerived> _inner;

    public DerivedDbContextFactory(IDbContextFactory<TDerived> inner) => _inner = inner;

    public ApplicationDbContext CreateDbContext() => _inner.CreateDbContext();

    public async Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => await _inner.CreateDbContextAsync(cancellationToken);
}

// Design-time factories used by the `dotnet ef` CLI. The SQL Server factory's
// connection string is a placeholder — `migrations add` never opens a connection.
// Set GOLFTRACKER_DESIGNTIME_CONNECTION to target a specific database with
// `dotnet ef database update` (e.g. a scratch copy for verification).

public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteApplicationDbContext>
{
    public SqliteApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("GOLFTRACKER_DESIGNTIME_CONNECTION")
            ?? "DataSource=Data/golfapp.db;Cache=Shared";
        var options = new DbContextOptionsBuilder<SqliteApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new SqliteApplicationDbContext(options);
    }
}

public sealed class SqlServerDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerApplicationDbContext>
{
    public SqlServerApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("GOLFTRACKER_DESIGNTIME_CONNECTION")
            ?? "Server=localhost;Database=GolfTrackerDesignTime;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<SqlServerApplicationDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new SqlServerApplicationDbContext(options);
    }
}
