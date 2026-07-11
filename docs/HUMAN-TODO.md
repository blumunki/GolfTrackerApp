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

## Production deploy — Phase-2 handicaps + Phase-3 competition schema

**Status:** Ready to deploy (Azure SQL compute has reset). **Context:** `docs/ARCHITECTURE.md` §5.1.

Local `main` is ahead of `origin` by a batch of built-and-tested commits (Phase-2 handicaps,
navigation/IA, WHS v2, and the Phase-3 competition schema). All tests green. Production is still
running the pre-handicaps code at baseline `20260611161345_InitialSqlServer`.

**Two migrations are pending on production** (both additive — new tables + nullable columns, so
no historical data is rewritten):
- `20260611202637_AddHandicapTables`
- `20260627171926_AddCompetitions`

Now that compute is available, migrations apply normally at startup — the `MigrateOnStartup=false`
workaround from the compute-exhausted period is **no longer needed** (it only existed to avoid
wasting scarce compute on a doomed attempt).

> ⚠️ **If you previously set the Azure App Service app setting `Database__MigrateOnStartup=false`,
> you MUST set it back to `true` (or delete it) before/at deploy.** Otherwise the new code runs
> against the old schema and every `Players` query fails with `Invalid column name
> 'PrimaryHandicapSource'` (Home + login break). See Troubleshooting below.

Deploy steps:

- [ ] **Back up** the production SQL Server database first (standard before any schema change).
- [x] Commit the latest `Data/Rounds.csv` / `Data/Scores.csv` changes
  (`f9674ac`).
- [ ] **Push `main`** → CI build+test gate → Azure deploys Web + Core.
- [ ] On first startup the new (resilient) code **applies both pending migrations** (compute is
  available; `MigrateOnStartup` defaults true). Watch the App Service log for
  `…migrations applied successfully`. Alternatively, deploy and apply on demand via
  **Admin → Database Migrations → Apply Pending Migrations**.
- [ ] Run **Admin → Handicap Backfill** to compute everyone's WHS index (v2 net double bogey) on
  real data.
- [ ] Verify: re-run `docs/sql-server-drift-check.sql` (clean schema), spot-check a handicap, and
  click through the app.

**Troubleshooting — `Invalid column name 'PrimaryHandicapSource'` (or any handicap/competition column):**
The new code deployed but the two migrations did NOT apply — almost always because
`Database__MigrateOnStartup=false` is still set in Azure App Service. Fix:
1. Azure App Service → Configuration → Application settings → set `Database__MigrateOnStartup` to
   `true` (or delete it) → Save (this restarts the app). On restart it applies both pending
   migrations; watch the log for `…migrations applied successfully`.
2. Alternatively, keep the flag as-is and apply on demand: browse to `/admin/database-migrations`
   (that page doesn't touch the `Players` table, so it loads even while the app is degraded) and
   click **Apply Pending Migrations**.
3. Then run **Admin → Handicap Backfill** and re-verify.
