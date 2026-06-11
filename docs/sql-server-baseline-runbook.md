# SQL Server Migration Baseline Runbook

This runbook transitions the existing production SQL Server database from
`EnsureCreated()` plus `EnsureNewTablesExistAsync()` to EF Core migrations.
The baseline migration represents the schema that production must already have:

- Migration ID: `20260611161345_InitialSqlServer`
- EF Core product version: `10.0.3`
- Migration context: `SqlServerApplicationDbContext`

**Completion status:** production reconciliation, baseline recording, and
zero-drift verification completed on 2026-06-11.

The scripts are deliberately separate. The drift check is read-only. The
reconciliation script makes only the reviewed production schema corrections.
The baseline script writes only the EF migration history table/row and never
creates or changes application tables.

## Safety Rules

1. A human database operator runs these scripts. Agents must never execute them
   against production.
2. Take and verify a production database backup before any reconciliation or
   baseline write.
3. Run each script while explicitly connected to the production application
   database. Neither script contains a `USE` statement.
4. Do not run the generated EF baseline migration against an existing database.
   It is a full-schema migration and would try to create existing objects.
5. Reconciliation must use `ON DELETE NO ACTION` for the optional
   `AspNetUsers.LinkedPlayerId` and `AiAuditLogs.AiChatSessionId` relationships.
   SQL Server rejects their model-level `SET NULL` actions because they create
   cycles or multiple cascade paths.
6. Do not deploy WORKLOG item `0-9` until the baseline row has been verified in
   production.

## Procedure

1. Take and verify a production database backup.
2. Run [`sql-server-drift-check.sql`](sql-server-drift-check.sql) and save its
   single result set with the deployment record.
3. Confirm its drift rows match the reviewed 64-row production report.
4. Confirm the orphan checks for `AspNetUsers.LinkedPlayerId` and
   `Scores.TeeSetId` both return zero.
5. Open
   [`sql-server-reconcile-baseline.sql`](sql-server-reconcile-baseline.sql).
6. Set its four operator confirmation values, then run it against production:
   - `@ConfirmedDatabaseName = N'sql-golftracker-db'`
   - `@ConfirmedBackupTaken = 1`
   - `@ConfirmedReviewedDriftErrorCount = 64`
   - `@ConfirmedOrphanChecksReturnedZero = 1`
7. Rerun the drift check. Continue only when the summary says
   `READY TO BASELINE` with zero errors.
8. Open [`sql-server-mark-baseline.sql`](sql-server-mark-baseline.sql).
9. Set `@ConfirmedDatabaseName` to the exact value returned by `DB_NAME()`.
10. Set `@ConfirmedCleanDriftCheckForMigrationId` to
   `20260611161345_InitialSqlServer`.
11. Set `@ConfirmedBackupTaken` to `1`, then run the script.
12. Rerun the drift check. Confirm the baseline row is reported and the schema
   remains clean.

The successful human execution is recorded in `docs/WORKLOG.md`; runtime
migration application was enabled by WORKLOG item `0-9`.

The reviewed reconciliation widens two TeeSet columns, removes legacy default
constraints, normalises legacy foreign-key/index names, and adds the missing
foreign keys and indexes. It does not update or delete application rows.
