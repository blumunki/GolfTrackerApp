using GolfTrackerApp.Core.Data;
using GolfTrackerApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GolfTrackerApp.Core.Services;

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<DatabaseMigrationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<DatabaseMigrationStatus> GetStatusAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // GetMigrations() reads the build's migration set without touching the database.
        var status = new DatabaseMigrationStatus
        {
            All = context.Database.GetMigrations().ToList(),
        };

        try
        {
            status.CanConnect = await context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reach the database while reading migration status.");
            return status;
        }

        if (!status.CanConnect)
        {
            return status;
        }

        status.Applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        status.Pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        return status;
    }

    public async Task<int> ApplyPendingMigrationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var pending = (await context.Database.GetPendingMigrationsAsync()).Count();
        if (pending == 0)
        {
            return 0;
        }

        _logger.LogInformation("Applying {Count} pending database migration(s) on demand...", pending);
        await context.Database.MigrateAsync();
        _logger.LogInformation("Applied {Count} pending database migration(s).", pending);
        return pending;
    }
}
