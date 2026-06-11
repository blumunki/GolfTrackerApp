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
| 0-4 | ScoreService + TeeSetService unit tests | 0 | In Progress | Claude | 2026-06-11 | |
| 0-5 | CI workflow (ci.yml build+test on PR/push; test gate in azure-deploy.yml) | 0 | Done | Claude | 2026-06-11 | No MAUI build in CI |
| 0-6 | Agent docs (CLAUDE/AGENTS/GEMINI.md, CONTRIBUTING-AGENTS.md, this board) | 0 | Done | Claude | 2026-06-11 | |
| 0-7 | Provider-split migration folders (Sqlite/SqlServer) + design-time factory | 0 | Available | | | Code only — no prod touch |
| 0-8 | SQL Server drift-check + baseline scripts in docs/ (human runs against prod) | 0 | Blocked | | | Blocked by 0-7 |
| 0-9 | Program.cs: replace EnsureCreated/EnsureNewTablesExistAsync with Migrate() for both providers | 0 | Blocked | | | Blocked by 0-8 baseline being applied to prod |
| 0-10 | ARCHITECTURE.md §12 status table + Phase 4 handicap restructure | 0 | Done | Claude | 2026-06-11 | Added §12.0, 4a/4b/4c increments, full WHS table, fixed dependency chain |
| 1-1 | Create GolfTrackerApp.Core project + move Models/Services/Data (no rename) | 1 | Blocked | | | Blocked by 0-1..0-4 (tests are the safety net) |
| 1-2 | Namespace rename GolfTrackerApp.Web.* → GolfTrackerApp.Core.* + fix usings | 1 | Blocked | | | Blocked by 1-1; zero logic changes allowed |
| 1-3 | Retarget test project to Core + update CI/deploy path filters | 1 | Blocked | | | Blocked by 1-2 |
| 2-1 | Handicap models (HandicapSource, ScoringDifferential, HandicapRecord) + dual migrations | 2 | Blocked | | | Blocked by 0-7/0-9 (dual-migration pipeline) |
| 2-2 | WHS math: pure ComputeDifferential/ComputeIndex + full unit tests (TDD) | 2 | Available | | | Pure functions — no DB dependency, can start anytime |
| 2-3 | HandicapService.OnRoundCompletedAsync + RoundService completion hook + integration test | 2 | Blocked | | | Blocked by 2-1, 2-2, 0-3 |
| 2-4 | Handicap backfill admin action (idempotent, reports n-of-m qualified) | 2 | Blocked | | | Blocked by 2-3 |
| 2-5 | Manual club handicap CRUD + HandicapsController | 2 | Blocked | | | Blocked by 2-1 |
| 2-6 | Web handicap dashboard (active handicaps, history chart, last-20 differentials) | 2 | Blocked | | | Blocked by 2-3, 2-5 |
| 2-7 | Mobile handicap page (DTOs + API service + dashboard) | 2 | Blocked | | | Blocked by 2-5 |

Phases 3–6 items are seeded when their phase starts — see the development plan summary in `docs/ARCHITECTURE.md` §12.

## Handoff Notes

(One `### <ID>` subsection per handed-off item: what's done, what remains, exact next step, gotchas.)
