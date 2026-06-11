# GolfTrackerApp — Agent Instructions (Claude)

> Your agent name for WORKLOG ownership is **Claude**. This file, `AGENTS.md` (Codex), and `GEMINI.md` (Gemini) are identical apart from this paragraph — see Working Agreement rule 6.

Golf performance tracking app for amateur golfers. Blazor Server web app (`GolfTrackerApp.Web`) hosting both the UI and the REST API (`/api/*`), consumed by a .NET MAUI Blazor Hybrid mobile app (`GolfTrackerApp.Mobile`). Shared interface-driven service layer, EF Core (SQLite in dev, SQL Server in prod).

## Commands

```bash
dotnet build GolfTrackerApp.Web/GolfTrackerApp.Web.csproj      # build web + API
dotnet test GolfTrackerApp.Web.Tests/GolfTrackerApp.Web.Tests.csproj   # run tests
dotnet run --project GolfTrackerApp.Web                        # run locally (SQLite)
```

Do not build `GolfTrackerApp.Mobile` unless the task requires it (MAUI workloads; use `run-android.sh` / `run-ios.sh` for device testing).

## Key documents

- `docs/ARCHITECTURE.md` — **single source of truth** for architecture and functionality
- `docs/CONTRIBUTING-AGENTS.md` — project conventions (services, migrations, testing, API compat)
- `docs/WORKLOG.md` — task board: claim work here before coding

## Working Agreement

1. **Claim before you code.** Open `docs/WORKLOG.md`, pick an `Available` item (or the item the user asked for), set Status=`In Progress` and Owner=your agent name, and commit that change first. Never work on an item owned by another agent unless its status is `Handoff` or `Available`.
2. **One chunk per session.** WORKLOG items are sized to finish in a single session. Do not expand scope mid-session; if you discover extra work, add it to WORKLOG.md as a new `Available` item instead of doing it now.
3. **Small diffs.** Several small commits beat one big one. Never mix refactoring with behaviour change in one commit. Never combine a schema migration with unrelated feature code.
4. **Leave the campsite clean.** Token limits mean a different agent may continue your work cold. Before ending a session: build and tests pass, everything is committed, and your WORKLOG row is `Done` — or `Handoff` with a Handoff Notes entry (done / remaining / exact next step / gotchas).
5. **Tests are the contract.** Run `dotnet test` before every commit. Changed or new service logic requires tests. A red test is never committed.
6. **Docs in the same commit.** Architectural or functional changes update `docs/ARCHITECTURE.md` (including the §12 Implementation Status table) in the same commit. Any instruction change in this file must be mirrored verbatim into the other two agent files (`CLAUDE.md` / `AGENTS.md` / `GEMINI.md`) in the same commit. Keep these files thin — anything beyond agent identity belongs in `docs/CONTRIBUTING-AGENTS.md` or `docs/ARCHITECTURE.md`.
7. **Read `docs/CONTRIBUTING-AGENTS.md`** before your first change. It contains hard rules (database dual-provider migrations, MudBlazor version split, API backward compatibility) that prevent production breakage.
