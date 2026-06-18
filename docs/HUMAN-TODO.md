# Human TODO

Tasks here require human access, judgment, or approval and must not be completed
automatically by an AI agent.

## Production SQL Server Migration Baseline

**Status:** Complete
**Unblocked:** WORKLOG item `0-9`
**Runbook:** [`sql-server-baseline-runbook.md`](sql-server-baseline-runbook.md)

Production previously used `EnsureCreated()` plus hand-written schema patches.
The human procedure below reconciled and recorded the existing schema as
migration `20260611161345_InitialSqlServer`, allowing application startup to
switch safely to EF Core migrations.

- [x] Take and verify a production SQL Server database backup.
- [x] Run [`sql-server-drift-check.sql`](sql-server-drift-check.sql) against the
  production application database.
- [x] Save the drift-check results with the deployment record.
- [x] Review the initial 64-row drift report.
- [x] Confirm orphan counts for `AspNetUsers.LinkedPlayerId` and
  `Scores.TeeSetId` are both zero.
- [x] Take and verify a fresh production SQL Server database backup immediately
  before reconciliation.
- [x] Configure and run
  [`sql-server-reconcile-baseline.sql`](sql-server-reconcile-baseline.sql).
- [x] Rerun the drift check until it reports `READY TO BASELINE` with zero
  errors.
- [x] Configure and run
  [`sql-server-mark-baseline.sql`](sql-server-mark-baseline.sql).
- [x] Rerun the drift check and confirm the baseline row is recorded and the
  schema remains clean.
- [x] Update `docs/WORKLOG.md` to record successful production baselining and
  unblock item `0-9`.

Completed against production on 2026-06-11.

## Production deploy of Phase-2 handicaps (pending — DB compute exhausted)

**Status:** Pending human action. **Context:** see `docs/ARCHITECTURE.md` §5.1 (startup resilience).

A large batch of commits (Phase-2 handicaps, navigation/IA, WHS v2) is built and tested
locally but **not yet deployed**. Production Azure SQL ran out of its monthly compute
allowance (resets at the start of the month), and applying the pending `AddHandicapTables`
migration itself consumes compute, so the deploy is deliberately deferred.

The app is now resilient to this (it starts degraded instead of crash-looping — WORKLOG 0-13).
When ready to release:

- [ ] Push `main` (this triggers CI + the Azure deploy).
- [ ] Set Azure App Service application setting `Database__MigrateOnStartup=false` **after** the
  deploy lands, so the app doesn't attempt the migration on a compute-starved DB. (Do not set
  it before the new code is deployed — the old code ignores the flag and would crash on restart.)
- [ ] When production compute is available (month reset): apply the schema via
  **Admin → Database Migrations → Apply Pending Migrations** (or set `MigrateOnStartup=true` and
  restart). Back up the DB first.
- [ ] Run **Admin → Handicap Backfill** to compute everyone's WHS index with the v2 method.
- [ ] (Optional) Re-run `docs/sql-server-drift-check.sql` to confirm a clean schema.
