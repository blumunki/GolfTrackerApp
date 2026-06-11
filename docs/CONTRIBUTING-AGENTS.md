# Contributing Guide for AI Agents

Shared conventions for all agents (Claude, Codex, Gemini) and humans working on GolfTrackerApp. The per-agent files (`CLAUDE.md`, `AGENTS.md`, `GEMINI.md`) hold the Working Agreement; this file holds the project-specific detail. `docs/ARCHITECTURE.md` is the single source of truth for architecture and functionality.

## 1. Service layer conventions

- Every service is an **interface + implementation pair** in `GolfTrackerApp.Core/Services/`: `I{Name}Service.cs` + `{Name}Service.cs`.
- Register as **scoped** in `Program.cs` DI.
- Services depend on **`IDbContextFactory<ApplicationDbContext>`** — never inject `ApplicationDbContext` directly (Blazor Server threading). Create a context per operation with `await _contextFactory.CreateDbContextAsync()` inside a `using`/`await using`.
- Business logic lives only in services. Blazor components and API controllers are thin callers — both must go through the same service so web and mobile behave identically.
- Keep calculation logic (e.g. handicap math, scoring formats) in **pure static-testable methods** where possible, separate from data access.

## 2. Database — CRITICAL dual-provider rules

Dev uses **SQLite** (EF migrations, `Data/golfapp.db`). Prod uses **SQL Server**.

Migrations are split per provider using derived context types (`GolfTrackerApp.Core/Data/ProviderContexts.cs`): `SqliteApplicationDbContext` owns `GolfTrackerApp.Core/Data/Migrations/Sqlite/`, `SqlServerApplicationDbContext` owns `GolfTrackerApp.Core/Data/Migrations/SqlServer/`. **Every schema change needs BOTH migrations** (run from the repository root):

```bash
dotnet ef migrations add <Name> --project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web --context SqliteApplicationDbContext --output-dir Data/Migrations/Sqlite
dotnet ef migrations add <Name> --project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web --context SqlServerApplicationDbContext --output-dir Data/Migrations/SqlServer
```

> ⚠️ **Transition state (until WORKLOG item 0-9 lands):** production SQL Server does **not** apply migrations at runtime yet — it still uses `EnsureCreated()` plus hand-written SQL in `EnsureNewTablesExistAsync()` in `Program.cs`. So until 0-9 lands, any new table/column needs the two migrations above **and** a matching block in `EnsureNewTablesExistAsync()` using SQL Server types (`NVARCHAR(n)` not `TEXT`, `INT` not `INTEGER`, `DATETIME2`, `BIT`; `ON DELETE NO ACTION` where multiple cascade paths exist). Remove this warning when 0-9 lands.

Set `GOLFTRACKER_DESIGNTIME_CONNECTION` and use `--project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web` to point `dotnet ef database update` at a scratch database for verification — never at production.

- Never run schema commands against production. Baseline/reconciliation scripts in `docs/` are executed by a human only.
- All new columns must be nullable or defaulted — no breaking changes to existing data.

## 3. Testing

- Test project: `GolfTrackerApp.Web.Tests` (xUnit, plain asserts).
- Use `Infrastructure/SqliteTestDbFactory` — an `IDbContextFactory<ApplicationDbContext>` over a single open in-memory SQLite connection. Dispose it (the fixture/test) to release the DB.
- Use `Infrastructure/TestDataBuilder` to seed Player/Club/Course/Hole/TeeSet/Round/Score graphs; extend it rather than hand-rolling entity graphs in tests.
- Changed or new service logic ⇒ tests in the same commit. Pure calculation code (handicaps, scoring) should be TDD'd.
- Run `dotnet test` before every commit.

## 4. API & mobile compatibility

- The mobile app consumes `/api/*` with JWT (`ApiAuth` scheme). **Controller changes must stay backward compatible** — mobile releases lag the server. Add fields; don't rename/remove or change semantics of existing JSON fields (mobile maps them with `[JsonPropertyName]`).
- New API surface follows `BaseApiController` patterns (`[Authorize(AuthenticationSchemes = "ApiAuth")]`, `GetCurrentUserId()`).
- Responses serialize with `System.Text.Json` + `ReferenceHandler.IgnoreCycles`.

## 5. UI conventions

- **MudBlazor version split: Web is on 8.x, Mobile is on 7.8.** Never bump Mobile's MudBlazor without on-device testing; never assume 8.x APIs exist in Mobile markup.
- Web CSS lives in `GolfTrackerApp.Web/wwwroot/css/` (components/layout/themes/utilities, `golf-` prefixed classes). No ad-hoc inline styles; no per-component `<style>` blocks.
- Mobile routing is a custom page switcher in `GolfTrackerApp.Mobile/Components/App.razor` (not Blazor `<Router>`).

## 6. Git & commits

- **Trunk-based, solo-developer workflow: work directly on `main`, locally.** No pull requests. Small, single-purpose commits (see Working Agreement rule 3).
- **Do not push unless the user asks you to.** Pushing `main` is the release step: it runs CI, and deploys the Web project to Azure when `GolfTrackerApp.Web/` changed (gated on tests). The user pushes when they're happy.
- For risky or multi-session work only (production DB scripts, namespace renames, large design passes): use a short-lived local branch, merge into `main` locally when green, delete the branch. Still no PR.
- Commit messages: imperative summary line ≤ 72 chars; reference the WORKLOG item ID where applicable (e.g. `0-1: add SqliteTestDbFactory`).

## 7. Roadmap & docs hygiene

- When you complete (or discover the true status of) roadmap work, update the **Implementation Status table in `docs/ARCHITECTURE.md` §12** in the same commit.
- Architectural changes (new entities, services, data flows, endpoints) update the relevant `ARCHITECTURE.md` sections in the same commit. Stale architecture docs are treated as bugs.

## 8. Writing good WORKLOG items

- Verb-first, single deliverable, verifiable done-condition (e.g. "Add ScoringDifferential model + dual migrations — done when `dotnet test` green and both migration folders contain the change").
- Sized for one agent session. If an item turns out too big, split it on the board before starting.
