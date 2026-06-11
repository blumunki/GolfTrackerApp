# Work Log — Agent Task Board

This board coordinates work between the AI agents on this project (Claude, Codex, Gemini) and human contributors.

## Protocol

1. **Claim before you code.** Pick an `Available` item, set Status to `In Progress` and Owner to your name, and commit that change before touching any other file.
2. **One item at a time.** Work items are sized for a single agent session. Do not expand scope mid-session — if you discover extra work, add it as a new `Available` row instead.
3. **Finish clean.** Before ending a session: build and tests pass, everything committed, and your row updated to `Done` — or `Handoff` with an entry in Handoff Notes below (what's done, what remains, exact next step, gotchas).
4. Never work on an item owned by another agent unless its status is `Handoff` or `Available`.
5. When a phase starts, seed its items from the plan in `docs/ARCHITECTURE.md` §12 / the development plan.

Statuses: `Available` · `In Progress` · `Handoff` · `Done` · `Blocked`

## Board

| ID | Work item | Phase | Status | Owner | Updated | Notes |
|----|-----------|-------|--------|-------|---------|-------|
| 0-1 | Create test project + SqliteTestDbFactory + TestDataBuilder + smoke tests | 0 | Done | Claude | 2026-06-11 | 3 smoke tests green |
| 0-2 | ReportService unit tests (dashboard stats, distributions, comparisons) | 0 | Done | Claude | 2026-06-11 | 13 tests; TestDataBuilder gained mixed-par courses + multi-player rounds |
| 0-3 | RoundService characterization tests (Add/Update, tee selections, access filtering, PrepareScorecard) | 0 | Done | Claude | 2026-06-11 | 19 tests; status-transition test marks the Phase 2 hook point |
| 0-4 | ScoreService + TeeSetService unit tests | 0 | Done | Claude | 2026-06-11 | 14 tests; found 0-11 and a second completion path (see 2-3 note) |
| 0-11 | Fix SaveScorecardAsync dropping TeeSetId from HoleScoreEntryModel | 0 | Done | Codex | 2026-06-11 | ScoreService now persists TeeSetId from scorecard entries; regression test updated |
| 0-5 | CI workflow (ci.yml build+test on PR/push; test gate in azure-deploy.yml) | 0 | Done | Claude | 2026-06-11 | No MAUI build in CI |
| 0-6 | Agent docs (CLAUDE/AGENTS/GEMINI.md, CONTRIBUTING-AGENTS.md, this board) | 0 | Done | Claude | 2026-06-11 | |
| 0-7 | Provider-split migration folders (Sqlite/SqlServer) + design-time factory | 0 | Done | Codex | 2026-06-11 | Derived contexts + split migrations documented; SQLite scratch migration chain and 49 tests verified |
| 0-8 | SQL Server drift-check + baseline scripts in docs/ (human runs against prod) | 0 | Done | Codex | 2026-06-11 | Production reconciled, baseline recorded, and zero-drift verification completed by human |
| 0-9 | Program.cs: replace EnsureCreated/EnsureNewTablesExistAsync with Migrate() for both providers | 0 | Done | Codex | 2026-06-11 | Both providers use MigrateAsync at startup; legacy SQL patcher removed; production baseline verified |
| 0-10 | ARCHITECTURE.md §12 status table + Phase 4 handicap restructure | 0 | Done | Claude | 2026-06-11 | Added §12.0, 4a/4b/4c increments, full WHS table, fixed dependency chain |
| 0-12 | Correct SQL Server baseline delete actions + add guarded production reconciliation script | 0 | Done | Codex | 2026-06-11 | Guarded reconciliation completed successfully against production; zero drift verified |
| 1-1 | Create GolfTrackerApp.Core project + move Models/Services/Data (no rename) | 1 | Done | Codex | 2026-06-11 | Core owns Models/Services/Data + both migration chains; namespaces preserved; 49 tests and both provider models verified |
| 1-2 | Namespace rename GolfTrackerApp.Web.* → GolfTrackerApp.Core.* + fix usings | 1 | Done | Claude | 2026-06-11 | 222-file mechanical rename incl. migration snapshot strings; has-pending-model-changes clean both providers; 49 tests green |
| 1-3 | Retarget test project to Core + update CI/deploy path filters | 1 | Done | Claude | 2026-06-11 | Tests reference Core directly; azure-deploy also triggers on Core/**; 49 tests green. Phase 1 complete |
| 2-1 | Handicap models (HandicapSource, ScoringDifferential, HandicapRecord) + dual migrations | 2 | In Progress | Claude | 2026-06-11 | Unblocked by completed dual-provider runtime migration pipeline |
| 2-2 | WHS math: pure ComputeDifferential/ComputeIndex + full unit tests (TDD) | 2 | Done | Claude | 2026-06-11 | WhsCalculator (+ ComputeAdjustedGrossScore, par+5 cap); 34 tests, every WHS table row covered; 83 total green |
| 2-3 | HandicapService.OnRoundCompletedAsync + RoundService completion hook + integration test | 2 | Blocked | | | Blocked by 2-1, 2-2, 0-3. NOTE: ScoreService.SaveScorecardAsync also completes rounds directly — the hook must cover both paths |
| 2-4 | Handicap backfill admin action (idempotent, reports n-of-m qualified) | 2 | Blocked | | | Blocked by 2-3 |
| 2-5 | Manual club handicap CRUD + HandicapsController | 2 | Blocked | | | Blocked by 2-1 |
| 2-6 | Web handicap dashboard (active handicaps, history chart, last-20 differentials) | 2 | Blocked | | | Blocked by 2-3, 2-5 |
| 2-7 | Mobile handicap page (DTOs + API service + dashboard) | 2 | Blocked | | | Blocked by 2-5 |

Phases 3–6 items are seeded when their phase starts — see the development plan summary in `docs/ARCHITECTURE.md` §12.

## Handoff Notes

(One `### <ID>` subsection per handed-off item: what's done, what remains, exact next step, gotchas.)
