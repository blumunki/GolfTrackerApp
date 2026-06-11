# Human TODO

Tasks here require human access, judgment, or approval and must not be completed
automatically by an AI agent.

## Production SQL Server Migration Baseline

**Status:** In progress
**Blocks:** WORKLOG item `0-9`
**Runbook:** [`sql-server-baseline-runbook.md`](sql-server-baseline-runbook.md)

Production currently uses `EnsureCreated()` plus hand-written schema patches.
Before the application can safely switch to EF Core `Migrate()`, a human with
authorized production database access must verify and record the existing
schema as migration `20260611161345_InitialSqlServer`.

- [ ] Take and verify a production SQL Server database backup.
- [x] Run [`sql-server-drift-check.sql`](sql-server-drift-check.sql) against the
  production application database.
- [ ] Save the drift-check results with the deployment record.
- [x] Review the initial 64-row drift report.
- [x] Confirm orphan counts for `AspNetUsers.LinkedPlayerId` and
  `Scores.TeeSetId` are both zero.
- [ ] Take and verify a fresh production SQL Server database backup immediately
  before reconciliation.
- [ ] Configure and run
  [`sql-server-reconcile-baseline.sql`](sql-server-reconcile-baseline.sql).
- [ ] Rerun the drift check until it reports `READY TO BASELINE` with zero
  errors.
- [ ] Configure and run
  [`sql-server-mark-baseline.sql`](sql-server-mark-baseline.sql).
- [ ] Rerun the drift check and confirm the baseline row is recorded and the
  schema remains clean.
- [ ] Update `docs/WORKLOG.md` to record successful production baselining and
  unblock item `0-9`.

Do not deploy `0-9` before every checkbox above is complete.
