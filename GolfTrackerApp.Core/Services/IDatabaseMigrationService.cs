using GolfTrackerApp.Core.Models;

namespace GolfTrackerApp.Core.Services;

public interface IDatabaseMigrationService
{
    /// <summary>
    /// Reports applied vs pending migrations for the active provider. Resilient to an
    /// unreachable database — returns CanConnect=false with the build's known migrations
    /// so the admin page is informative even while the DB is down.
    /// </summary>
    Task<DatabaseMigrationStatus> GetStatusAsync();

    /// <summary>
    /// Applies any pending migrations on demand (the "apply when the database is available"
    /// lever that complements MigrateOnStartup). Returns the number of migrations applied;
    /// 0 when already up to date. Throws if the database is unavailable.
    /// </summary>
    Task<int> ApplyPendingMigrationsAsync();
}
