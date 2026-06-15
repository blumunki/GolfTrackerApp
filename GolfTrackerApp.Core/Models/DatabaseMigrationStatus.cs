namespace GolfTrackerApp.Core.Models;

/// <summary>Snapshot of the database's migration state for the admin Database Migrations page.</summary>
public class DatabaseMigrationStatus
{
    /// <summary>Whether the database is currently reachable.</summary>
    public bool CanConnect { get; set; }

    /// <summary>Every migration defined in the build (does not require a DB connection).</summary>
    public IReadOnlyList<string> All { get; set; } = Array.Empty<string>();

    /// <summary>Migrations already applied to the database (empty when unreachable).</summary>
    public IReadOnlyList<string> Applied { get; set; } = Array.Empty<string>();

    /// <summary>Migrations defined in the build but not yet applied (empty when unreachable).</summary>
    public IReadOnlyList<string> Pending { get; set; } = Array.Empty<string>();

    public bool HasPending => Pending.Count > 0;
}
