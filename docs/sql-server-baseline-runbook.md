# SQL Server Migration Baseline Runbook

This runbook transitions the existing production SQL Server database from
`EnsureCreated()` plus `EnsureNewTablesExistAsync()` to EF Core migrations.
The baseline migration represents the schema that production must already have:

- Migration ID: `20260611161345_InitialSqlServer`
- EF Core product version: `10.0.3`
- Migration context: `SqlServerApplicationDbContext`

The scripts are deliberately separate. The drift check is read-only. The
baseline script writes only the EF migration history table/row and never creates
or changes application tables.

## Safety Rules

1. A human database operator runs these scripts. Agents must never execute them
   against production.
2. Take and verify a production database backup before any reconciliation or
   baseline write.
3. Run each script while explicitly connected to the production application
   database. Neither script contains a `USE` statement.
4. Do not run the generated EF baseline migration against an existing database.
   It is a full-schema migration and would try to create existing objects.
5. Do not deploy WORKLOG item `0-9` until the baseline row has been verified in
   production.

## Procedure

1. Run [`sql-server-drift-check.sql`](sql-server-drift-check.sql).
2. Save its result sets with the deployment record.
3. Continue only when the summary says `READY TO BASELINE` with zero errors.
4. If drift is reported, stop. Review each difference and prepare a separate,
   reviewed reconciliation script. Back up, reconcile, and rerun the drift
   check until it is clean.
5. Open [`sql-server-mark-baseline.sql`](sql-server-mark-baseline.sql).
6. Set `@ConfirmedDatabaseName` to the exact value returned by `DB_NAME()`.
7. Set `@ConfirmedCleanDriftCheckForMigrationId` to
   `20260611161345_InitialSqlServer`.
8. Set `@ConfirmedBackupTaken` to `1`, then run the script.
9. Rerun the drift check. Confirm the baseline row is reported and the schema
   remains clean.

After successful human execution, update `docs/WORKLOG.md` to record that the
production baseline was applied. Only then may `0-9` be unblocked.

The first drift check is expected to find differences created by the legacy
hand-written startup SQL, such as default constraints, object names, and some
column definitions. These are real differences from the EF model; do not waive
them or mark the baseline until they have been reviewed and reconciled.
