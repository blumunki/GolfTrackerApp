# Golf Tracker App — System Architecture

## 1. Overview

Golf Tracker App is a cross-platform golf performance tracking system consisting of two front-end applications — a Blazor Server web app and a .NET MAUI Blazor Hybrid mobile app — sharing a centralised API backend hosted by the web project and a business/data layer compiled into `GolfTrackerApp.Core`.

```
┌─────────────────────────────────────────────────────────────────┐
│                        End Users                                │
│                                                                 │
│    ┌──────────────┐                    ┌──────────────────┐     │
│    │  Web Browser  │                    │  Mobile Device   │     │
│    │  (Desktop/    │                    │  (iOS / Android) │     │
│    │   Mobile)     │                    │                  │     │
│    └──────┬───────┘                    └────────┬─────────┘     │
└───────────┼────────────────────────────────────┼────────────────┘
            │ Blazor Server                       │ HTTP + JWT
            │ (SignalR WebSocket)                 │
            ▼                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GolfTrackerApp.Web                            │
│                                                                 │
│  ┌──────────────────┐     ┌──────────────────────────────────┐ │
│  │  Blazor Server   │     │  ASP.NET Core API Controllers    │ │
│  │  Components      │     │  /api/auth, /api/rounds, etc.    │ │
│  │  (Pages, Layout, │     │  ┌────────────────────────────┐  │ │
│  │   Shared)        │     │  │  JWT Bearer Auth (ApiAuth)  │  │ │
│  └────────┬─────────┘     │  └────────────────────────────┘  │ │
│           │                └──────────────┬───────────────────┘ │
│           │  Direct DI injection          │  Calls via DI       │
│           ▼                               ▼                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                  Service Layer                            │   │
│  │  IGolfClubService, IGolfCourseService, IRoundService,     │   │
│  │  IPlayerService, IReportService, IScoreService,           │   │
│  │  IHoleService, IConnectionService, IMergeService,         │   │
│  │  INotificationService, IRoundWorkflowService,             │   │
│  │  IAiInsightService, IAiRoutingService, IAiAuditService,   │   │
│  │  IAiChatService, IAiProviderSettingsService               │   │
│  └──────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              AI Provider Layer (8 providers)              │   │
│  │  OpenAI, Anthropic, Gemini, Grok, Mistral,                │   │
│  │  DeepSeek, MetaLlama, Manus                               │   │
│  │  (priority-based routing + circuit breaker failover)       │   │
│  └──────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              Entity Framework Core 10                     │   │
│  │              IDbContextFactory<ApplicationDbContext>       │   │
│  └──────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              SQLite (Dev) / SQL Server (Prod)             │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    GolfTrackerApp.Mobile                         │
│                                                                 │
│  ┌──────────────────┐     ┌──────────────────────────────────┐ │
│  │  MAUI Blazor     │     │  API Client Services              │ │
│  │  Hybrid Pages    │────▶│  GolfClubApiService,              │ │
│  │  (Razor + C#)    │     │  RoundApiService,                 │ │
│  │                  │     │  PlayerApiService, etc.            │ │
│  └──────────────────┘     └──────────────┬───────────────────┘ │
│                                          │ HttpClient + JWT     │
│  ┌──────────────────┐                    │                      │
│  │  Google Sign-In  │                    │                      │
│  │  + Auth State    │                    │                      │
│  └──────────────────┘                    │                      │
└──────────────────────────────────────────┼──────────────────────┘
                                           │
                                           ▼
                                   GolfTrackerApp.Web
                                   /api/* endpoints
```

The logical service, AI provider, and Entity Framework layers shown inside the Web host are implemented by the referenced `GolfTrackerApp.Core` project under `GolfTrackerApp.Core.*` namespaces.

## 2. Design Principles

### 2.1 Centralised Business Logic
All business logic lives in the Core project's **Service Layer**. Both the web Blazor components and the API controllers consume the same services via dependency injection. This guarantees:
- Identical behaviour on web and mobile
- A single place to fix bugs or add features
- No risk of logic drift between platforms

### 2.2 API-First for Mobile
The mobile app communicates exclusively through REST API endpoints. Every feature available on mobile has a corresponding API controller action. This creates a clean contract between client and server.

### 2.3 Future-Proof API Separation
The current architecture intentionally hosts API controllers within the Web project for MVP simplicity. Models, services, and data access are extracted into `GolfTrackerApp.Core`, and the service layer is fully interface-driven (`IGolfClubService`, `IRoundService`, etc.), making it straightforward to:
1. Move API controllers into a dedicated API project
2. Deploy web and API independently

These later refactors require no changes to business logic — only namespaces, DI registration, and project references.

## 3. Component Architecture

### 3.1 Core Project (GolfTrackerApp.Core)

```
GolfTrackerApp.Core/
├── Models/                             # Domain models (EF entities + DTOs)
├── Services/                           # Business logic (interfaces + implementations)
│   └── AiProviders/                    # AI provider implementations
└── Data/
    ├── ApplicationDbContext.cs         # EF Core context (Identity + domain entities)
    ├── ProviderContexts.cs             # Provider-specific contexts + design-time factories
    ├── ApplicationUser.cs              # Identity user
    ├── SeedData.cs                     # Initial data seeding
    └── Migrations/                     # Provider-split EF Core migrations
        ├── Sqlite/                     # Development migration chain
        └── SqlServer/                  # Production migration chain
```

Core source files use `GolfTrackerApp.Core.*` namespaces (`Models`, `Services`, `Data`, and the migration namespaces).

### 3.2 Web Project (GolfTrackerApp.Web)

```
GolfTrackerApp.Web/
├── Program.cs                          # Host configuration, DI, auth, middleware
├── Controllers/                        # REST API surface
│   ├── BaseApiController.cs            # Shared JWT auth + user ID extraction
│   ├── AuthController.cs              # Login, register, Google sign-in → JWT
│   ├── ConnectionsController.cs       # Player-to-player social connections
│   ├── DashboardController.cs         # Dashboard stats for mobile
│   ├── GolfClubsController.cs         # Golf club CRUD
│   ├── GolfCoursesController.cs       # Golf course CRUD
│   ├── InsightsController.cs          # AI insights + chat API endpoints
│   ├── MergeController.cs             # Managed player merge workflow
│   ├── NotificationsController.cs     # User notification CRUD + mark-read
│   ├── PlayersController.cs           # Player CRUD + reports
│   ├── ReportsController.cs           # Aggregated reporting endpoints
│   └── RoundsController.cs            # Round CRUD
├── Data/                               # Host-owned CSV seed assets + local SQLite database
├── Components/
│   ├── Pages/                        # Blazor Server page components
│   │   ├── Home.razor                # Dashboard with AI insights widget
│   │   ├── AiChat.razor              # AI coach chat with persistent sessions
│   │   ├── GolfClubs/                # Club list, add, edit, details
│   │   ├── GolfCourses/              # Course list, add, edit, details
│   │   ├── Players/                  # Player list, add, edit, report
│   │   ├── Rounds/                   # Round list, record, details
│   │   └── Admin/                    # Dashboard, Users, Players, Content Health,
│   │                                 # Connections, Notifications, Audit, Data Migration,
│   │                                 # AI Providers, AI Usage
│   ├── Layout/                       # MainLayout + NavMenu
│   ├── Shared/                       # Dialogs, reusable components
│   └── Account/                      # Identity UI pages (scaffolded)
└── wwwroot/css/                      # Centralised CSS architecture
    ├── components/                   # Component-specific styles
    ├── layout/                       # Layout styles
    ├── themes/                       # Variables, MudBlazor overrides
    └── utilities/                    # Utility classes
```

### 3.3 Mobile Project (GolfTrackerApp.Mobile)

```
GolfTrackerApp.Mobile/
├── MauiProgram.cs                     # MAUI host, DI, HttpClient config
├── App.xaml / App.xaml.cs             # MAUI application entry
├── MainPage.xaml / .cs                # BlazorWebView host
├── Components/
│   ├── App.razor                      # Root component (custom page routing + bottom nav)
│   ├── Dashboard/                     # Dashboard widget components
│   │   ├── CourseDiaryWidget.razor
│   │   ├── HeroStatsWidget.razor
│   │   ├── AiInsightsWidget.razor
│   │   ├── ParPerformanceWidget.razor
│   │   └── ScoringBreakdownWidget.razor
│   ├── Shared/
│   │   └── MobileRoundDetailDialog.razor
│   └── Pages/                         # Page components
│       ├── Home.razor                 # Dashboard
│       ├── LoginPage.razor            # Email/password + Google sign-in
│       ├── GolfClubsPage.razor        # Club list + create dialog
│       ├── ClubDetailPage.razor       # Club detail + edit/add course
│       ├── CourseDetailPage.razor      # Course detail + edit dialog
│       ├── RoundsPage.razor           # Round list
│       ├── RecordRoundPage.razor      # Round recording workflow
│       ├── RoundDetailPage.razor      # Round detail + edit scores/delete
│       ├── PlayersPage.razor          # Player management + connections
│       ├── NotificationsPage.razor    # In-app notifications
│       ├── PlayerReportPage.razor     # Player stats report
│       └── AiChatPage.razor           # AI coach chat with sessions
├── Models/                            # Mobile DTOs
│   ├── Round.cs, Player.cs
│   ├── GolfClub.cs, GolfCourse.cs
│   └── CreateRoundRequest.cs
├── Services/
│   ├── AuthenticationStateService.cs  # JWT token storage + auth state
│   ├── GoogleAuthenticationService.cs # Google OAuth flow
│   └── Api/                           # HTTP API clients
│       ├── DashboardApiService.cs
│       ├── RoundApiService.cs
│       ├── PlayerApiService.cs
│       ├── GolfClubApiService.cs
│       ├── GolfCourseApiService.cs
│       ├── PlayerReportApiService.cs
│       ├── ConnectionApiService.cs
│       ├── NotificationApiService.cs  # Notification API client
│       └── InsightsApiService.cs      # AI insights + chat API client
└── Resources/                         # App icon, splash screen, fonts
```

## 4. Data Flow

### 4.1 Web — Direct Service Access
```
User Action → Blazor Component → Service (via DI) → EF Core → Database
```
Web Blazor components inject services directly. No HTTP overhead. The service call and the page render happen in the same server process.

### 4.2 Mobile — API Client Pattern
```
User Action → Blazor Component → API Service → HTTP Request → API Controller → Service → EF Core → Database
                                                    ↓
                                              JWT Validated
```
Mobile components inject API client services (e.g., `RoundApiService`) which make HTTP calls to the Web project's API controllers. The controllers then call the same service layer used by web components.

### 4.3 Authentication Flow

**Web:**
```
User → Identity Login/Google OAuth → Cookie set → Blazor AuthenticationStateProvider
```

**Mobile:**
```
User → Google Sign-In (WebAuthenticator) → POST /api/auth/google-signin
     → JWT returned → Stored in Preferences → Attached to all API calls via HttpClient handler
```

## 5. Database Schema

The application uses Entity Framework Core with the following primary entities:

```
ApplicationUser (ASP.NET Identity)
    ├── N:1 → Player (LinkedPlayerId — cached FK to user's own player record)
    ├── 1:N → Player (CreatedByApplicationUserId — players this user created)
    ├── 1:N → PlayerConnection (RequestingUserId / TargetUserId)
    ├── 1:N → PlayerMergeRequest (RequestingUserId / TargetUserId)
    ├── 1:N → Notification
    ├── 1:N → AiChatSession
    ├── 1:N → AiAuditLog
    └── AiInsightsOptOut (bool — user opt-out toggle)

Player
    ├── 1:N → RoundPlayer (junction)
    ├── 1:N → Score
    └── 1:N → PlayerMergeRequest (SourcePlayerId / TargetPlayerId)

GolfClub
    └── 1:N → GolfCourse
                  └── 1:N → Hole

Round
    ├── N:1 → GolfCourse
    ├── 1:N → RoundPlayer (junction to Player)
    └── 1:N → Score
                  ├── N:1 → Player
                  └── N:1 → Hole

AiChatSession
    ├── N:1 → ApplicationUser
    ├── 1:N → AiChatSessionMessage
    └── 1:N → AiAuditLog (optional FK)

AiAuditLog
    ├── N:1 → ApplicationUser
    └── N:1 → AiChatSession (nullable)

AiProviderSettings
    └── ProviderName (unique), Enabled, Priority, UpdatedAt
```

**Database providers:**
- **Development**: SQLite (`Data/golfapp.db`)
- **Production**: SQL Server (connection string in `appsettings.Production.json`)

`IDbContextFactory<ApplicationDbContext>` is used throughout services for Blazor Server compatibility (avoids DbContext threading issues).

### 5.1 Database Provider Differences (IMPORTANT for AI Agents)

Development and production use **different database providers** with different capabilities and schema management strategies. Any database schema change must account for both.

| Aspect | Development (SQLite) | Production (SQL Server) |
|--------|---------------------|------------------------|
| **Provider** | `Microsoft.EntityFrameworkCore.Sqlite` | `Microsoft.EntityFrameworkCore.SqlServer` |
| **Migration context** | `SqliteApplicationDbContext` → `GolfTrackerApp.Core/Data/Migrations/Sqlite/` | `SqlServerApplicationDbContext` → `GolfTrackerApp.Core/Data/Migrations/SqlServer/` |
| **Runtime schema management** | EF Core Migrations (`context.Database.Migrate()`) | `EnsureCreated()` + manual SQL in `EnsureNewTablesExistAsync()` — **until WORKLOG 0-9 lands**, then `Migrate()` |
| **Column types** | `INTEGER`, `TEXT`, `REAL` | `INT`, `NVARCHAR(n)`, `DATETIME2`, `BIT`, etc. |
| **Cascade deletes** | Generally permissive | Strict — rejects `ON DELETE SET NULL` / `CASCADE` if it creates multiple cascade paths |
| **Config key** | `"DatabaseProvider": "Sqlite"` (in `appsettings.Development.json`) | `"DatabaseProvider": "SqlServer"` (in `appsettings.Production.json`) |

Migrations are split per provider via derived context types in `GolfTrackerApp.Core/Data/ProviderContexts.cs` (EF Core discovers all migrations attributed to a context type in the migrations assembly, so each provider's set is attached to its own derived context). Application code is unaffected — DI forwards `ApplicationDbContext` / `IDbContextFactory<ApplicationDbContext>` to the active provider's context (`Program.cs`).

Production must be reconciled and marked with the SQL Server baseline before runtime migration application is enabled. Follow `docs/sql-server-baseline-runbook.md`: its drift check is read-only and compares the model's tables, columns, defaults, primary keys, indexes, and foreign keys to `20260611161345_InitialSqlServer`; its guarded marker writes only the matching `__EFMigrationsHistory` row after a human confirms a clean check and verified backup. WORKLOG item `0-9` stays blocked until that human-run production step is recorded.

**When making any database schema change, you MUST:**

1. **Create BOTH migrations** (from the repository root):
   ```bash
   dotnet ef migrations add <Name> --project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web --context SqliteApplicationDbContext --output-dir Data/Migrations/Sqlite
   dotnet ef migrations add <Name> --project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web --context SqlServerApplicationDbContext --output-dir Data/Migrations/SqlServer
   ```

2. **Transition state (until WORKLOG 0-9 lands):** also update `EnsureNewTablesExistAsync()` in `Program.cs` for SQL Server production:
   - New tables: Add a `TableExistsAsync` check and `CREATE TABLE` with SQL Server types
   - New columns on existing tables: Add a `ColumnExistsAsync` check and `ALTER TABLE ... ADD`
   - Use `NVARCHAR(n)` not `TEXT`, `INT` not `INTEGER`, `DATETIME2` not `TEXT`, `BIT` not `INTEGER`

3. **Avoid cascade conflicts on SQL Server:**
   - Use `ON DELETE NO ACTION` for foreign keys where multiple cascade paths exist (e.g., `AspNetUsers` ↔ `Players`)
   - `ON DELETE CASCADE` is only safe when there's a single path from parent to dependent
   - `ON DELETE SET NULL` also triggers the cascade-path check on SQL Server

4. **Test both providers** before deploying schema changes. For SQLite, apply the chain to a scratch DB: set `GOLFTRACKER_DESIGNTIME_CONNECTION` and run `dotnet ef database update --project GolfTrackerApp.Core --startup-project GolfTrackerApp.Web --context SqliteApplicationDbContext`. Never point it at production.

## 6. Service Layer Design

All services follow the same pattern:
- **Interface** in `GolfTrackerApp.Core/Services/I{Name}Service.cs`
- **Implementation** in `GolfTrackerApp.Core/Services/{Name}Service.cs`
- **Dependency**: `IDbContextFactory<ApplicationDbContext>` injected via constructor
- **Lifetime**: All registered as scoped services in DI

Key services and their responsibilities:

| Service | Responsibility |
|---------|---------------|
| `IGolfClubService` | CRUD for golf clubs, search |
| `IGolfCourseService` | CRUD for courses, search, club-filtered queries |
| `IRoundService` | Round CRUD, player linking, recent rounds, counts |
| `IPlayerService` | Player CRUD, search, user-scoped queries |
| `IReportService` | Dashboard stats, scoring distributions, comparisons, course history |
| `IScoreService` | Score CRUD, scorecard save |
| `IHoleService` | Hole CRUD for courses |
| `IConnectionService` | Social connections between users |
| `IMergeService` | Merge managed player data into connected accounts |
| `INotificationService` | User notification lifecycle |
| `IRoundWorkflowService` | Orchestrates multi-step round recording |
| `IAiInsightService` | Golf-specific AI insight generation (dashboard, report, club, course, chat) |
| `IAiRoutingService` | Multi-provider routing with priority ordering + circuit breaker failover |
| `IAiAuditService` | Audit logging, rate limiting, usage counts, retention cleanup |
| `IAiChatService` | Persistent chat session CRUD (create, resume, archive) |
| `IAiProviderSettingsService` | Admin-managed provider on/off + priority (DB-backed) |

## 7. API Design

### 7.1 Authentication Schemes
- **Cookie** (`Identity.Application`): Used by web Blazor Server pages
- **JWT Bearer** (`ApiAuth`): Used by mobile app and API-only endpoints
- **Google OAuth**: Federated login (web uses ASP.NET Identity integration; mobile uses `WebAuthenticator`)

### 7.2 Controller Patterns
- `BaseApiController`: Abstract base with `[Authorize(AuthenticationSchemes = "ApiAuth")]`, provides `GetCurrentUserId()` helper
- Auth endpoints (`/api/auth/*`): Unauthenticated, issue JWT tokens
- Reference data (`/api/golfclubs`, `/api/golfcourses` GET): Publicly accessible
- User data (`/api/rounds`, `/api/players`, `/api/dashboard`, `/api/reports`): JWT-protected
- AI endpoints (`/api/insights/*`): JWT-protected, rate-limited per user

### 7.3 Serialisation
- JSON with `System.Text.Json`
- `ReferenceHandler.IgnoreCycles` on responses with circular navigation properties
- Mobile models use `[JsonPropertyName]` attributes to map API response fields

## 8. CSS Architecture (Web)

```
wwwroot/css/
├── components/          # Per-feature styles
│   ├── ai-insights.css             # AI widget cards, shimmer loading, provider badges
│   ├── golf-chat.css               # AI chat page bubbles, input bar, session list
│   ├── golf-clubs.css              # Club/course list + detail pages
│   ├── golf-dashboard.css          # Dashboard widgets
│   ├── golf-rounds.css             # Round list + detail
│   ├── golf-scorecard.css          # Scorecard entry UI
│   ├── golf-report.css             # Player report pages
│   ├── golf-premium-components.css
│   ├── notifications.css
│   └── players.css                 # Players page
├── layout/
│   ├── main-layout.css
│   └── navigation.css
├── themes/
│   ├── golf-variables.css      # CSS custom properties
│   ├── golf-premium.css
│   └── mudblazor-overrides.css
└── utilities/
    └── golf-utilities.css
```

The design system uses:
- Dark gradient headers (`#1a1a2e → #2d3748`)
- Card-based layouts with stat accent colours
- MudBlazor component library with targeted overrides
- `golf-` prefixed class names

## 9. Mobile Routing

The mobile app uses a **custom page switcher** in `App.razor` rather than Blazor's `<Router>`. Navigation works via:

1. Component calls `NavigationManager.NavigateTo("page-name/param")`
2. `App.razor` handles `LocationChanged` event
3. URI is parsed and `currentPage` string is updated
4. `switch(currentPage)` renders the appropriate component, passing parameters

This pattern was chosen for full control over transitions and bottom navigation state in the MAUI hybrid context.

## 10. AI Insights Architecture

The AI Insights feature provides AI-generated golf performance analysis across the entire application.

### 10.1 Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                    AiInsightService                         │
│  (Golf-specific orchestration: prompts, caching, context)  │
│                                                            │
│  Methods: Dashboard, PlayerReport, Club, Course, Chat      │
│  Data Freshness: watermark-based caching (not time-based)  │
│  User Control: opt-out check via ApplicationUser flag      │
└────────────────────┬───────────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────────────┐
│                   AiRoutingService                          │
│  (Multi-provider routing with failover)                    │
│                                                            │
│  • Ordered by priority from AiProviderSettings (DB)        │
│  • Circuit breaker: 5-min cooldown on failed providers     │
│  • Falls through to next provider on failure               │
└────────────────────┬───────────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│  OpenAI  │  │Anthropic │  │ Gemini   │  ... + Grok, Mistral,
│ (GPT-4o  │  │ (Claude  │  │(Gemini   │      DeepSeek, MetaLlama,
│  mini)   │  │ Sonnet 4)│  │ 3.1)     │      Manus
└──────────┘  └──────────┘  └──────────┘

Cross-cutting:
┌────────────────────────────────┐  ┌─────────────────────────┐
│       AiAuditService           │  │   AiChatService          │
│  • Per-request logging         │  │  • Persistent sessions   │
│  • Rate limiting (20/hr/user)  │  │  • Message history       │
│  • Usage stats for admin       │  │  • Session archival      │
│  • Retention cleanup (90 days) │  │                          │
└────────────────────────────────┘  └─────────────────────────┘
```

### 10.2 Provider Configuration

Provider settings are split across two sources:

| Source | What it stores | Managed by |
|--------|---------------|-----------|
| `appsettings.json` | Model name, endpoint URL, timeout | Developer (committed to repo) |
| `dotnet user-secrets` / env vars | API keys | Ops / deployment pipeline |
| `AiProviderSettings` DB table | Enabled/disabled, priority order | Admin UI at runtime |

On startup, the `AiProviderSettings` table is seeded from config (all providers disabled by default). Admins enable providers and set priority via `/admin/ai-providers`.

### 10.3 Data Freshness (Smart Caching)

Insights are cached against a **data watermark** — the timestamp of the user's most recent round. If no new rounds have been played since the last insight was generated, the cached result is returned without calling an AI provider. After a configurable period with no new data (`StaleInsightMonths: 3`), a staleness message is shown.

### 10.4 User Controls

- **Opt-out toggle**: Users can disable AI Insights via Account Settings (`AiInsightsOptOut` on `ApplicationUser`). All insight methods check this flag and return a friendly message if opted out.
- **Rate limiting**: 20 AI requests per user per hour (configurable), enforced via `AiAuditService`.

### 10.5 AI Configuration

```json
"AiInsights": {
  "Enabled": false,
  "MaxTokens": 500,
  "Temperature": 0.7,
  "CacheMinutes": 60,
  "StaleInsightMonths": 3,
  "RateLimitPerUserPerHour": 20,
  "AuditLogging": {
    "Enabled": true,
    "LogPrompts": true,
    "LogResponses": true,
    "RetentionDays": 90
  }
}
```

### 10.6 AI Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/insights/dashboard` | GET | Dashboard performance analysis |
| `/api/insights/player-report/{playerId}` | GET | Player report with optional course/holes filter |
| `/api/insights/club/{clubId}` | GET | Club-specific analysis |
| `/api/insights/course/{courseId}` | GET | Course-specific analysis |
| `/api/insights/chat` | POST | Send chat message (body: `{ message, sessionId? }`) |
| `/api/insights/sessions` | GET | List user's chat sessions |
| `/api/insights/sessions/{id}` | GET | Get session with message history |

### 10.7 Admin Pages

- **Admin Dashboard** (`/admin`): System overview — user/player/round/course/connection/merge counts, recent users, content health summary, quick links
- **User Management** (`/admin/users`): Search/filter users, view linked players, promote/demote admin roles, see AI opt-out and email confirmation status
- **Player Management** (`/admin/players`): Search/filter players, inline editing (name, handicap), view linked accounts, round counts, linked/unlinked breakdown
- **Content Health** (`/admin/content-health`): Health score, clubs without courses, courses without holes, hole count mismatches, par mismatches, duplicate stroke indices
- **Connections & Merges** (`/admin/connections`): All connections/merge requests with status filters, pending counts, tabbed view
- **System Notifications** (`/admin/notifications`): View all user notifications, type breakdown, read/unread stats, filterable by type and status
- **Audit Trail** (`/admin/audit`): AI audit logs with filters (type, provider, status, time range), expandable prompt/response detail, token summaries
- **Data Migration** (`/admin/datamigration`): Quick sync from CSV, manual file upload for reference data and rounds/scores
- **AI Providers** (`/admin/ai-providers`): Enable/disable providers, set priority order, view API key status
- **AI Usage** (`/admin/ai-usage`): Usage statistics, token consumption, provider breakdown, audit log viewer

## 11. Deployment

### Web
- Standard ASP.NET Core deployment (IIS, Azure App Service, Docker)
- `web.config` included for IIS hosting
- `appsettings.Production.json` for production connection strings

### Mobile
- **Android**: `dotnet build -f net10.0-android` → APK/AAB
- **iOS**: `dotnet build -f net10.0-ios` → IPA (requires Xcode)
- Mobile connects to the deployed Web API via `DevConfiguration.generated.cs` base URL

## 12. Feature Roadmap

Planned features organised by priority tier. Each item includes the affected platform(s).

### 12.0 Implementation Status (keep this table accurate)

> **Rule for all contributors (human or AI agent):** when you complete roadmap work — or discover that this table is wrong — update it in the same commit. Work items and ownership live in `docs/WORKLOG.md`.

| Phase | Feature area | Status | Notes |
|-------|-------------|--------|-------|
| — | Mobile feature parity (§12.1–12.2) | ✅ Done | |
| — | Admin area (§12.3) | ✅ Done | |
| — | Live Round Mode | ✅ Done | Single-device scorecard entry; no real-time multi-player sync, no hole maps |
| 1 | Tee Sets & Course Ratings | ✅ Done | TeeSet/HoleTee models, per-player tee selection, rating/slope fields |
| 2 | Golf Societies & Memberships | ✅ Done | Models, services, controllers, web + mobile pages. Feels thin only because competitions/handicaps don't exist yet |
| 3 | Competitions & Scoring Formats | ❌ Not started | Specced in §12.5 only |
| 4a | Personal WHS handicap (differentials + index + backfill) | 🚧 In progress | WHS math done (`WhsCalculator`, pure + unit-tested); models, persistence, and completion hook pending. Does **not** require Phase 3 |
| 4b | Manual club/regional handicaps + handicap UI | ❌ Not started | |
| 4c | Society handicaps | ❌ Not started | Requires Phase 3 (competition-linked rounds) |
| 0 | Engineering foundations (tests, real migrations both providers, CI test gate, agent docs) | 🚧 In progress | See `docs/WORKLOG.md` items 0-1…0-10 |
| — | Core project extraction | ✅ Done | Models, services, data, and migrations live in `GolfTrackerApp.Core` (`GolfTrackerApp.Core.*` namespaces); tests reference Core directly; deploy triggers on Web + Core paths |
| — | Proactive AI coaching (background jobs) | ❌ Not started | AI layer is user-triggered only today |
| — | Course data expansion (OSM geometry, AI-assisted entry) + hole visuals | ❌ Not started | |

### 12.1 Mobile Feature Parity — Critical

| Feature | Status | Description | Platform |
|---------|--------|-------------|----------|
| Edit Round | ✅ Done | Inline score editing with +/− controls, delete with confirmation | Mobile |
| Player Connections | ✅ Done | Already implemented — search, send/accept/decline requests | Mobile |
| Notifications | ✅ Done | NotificationsController + NotificationApiService + NotificationsPage with badge | Mobile |
| Email/Password Auth | ✅ Done | Login + registration wired to AuthController (was TODO stubs) | Mobile |

### 12.2 Mobile Feature Parity — High Value

| Feature | Status | Description | Platform |
|---------|--------|-------------|----------|
| Add/Edit Clubs & Courses | ✅ Done | Create club dialog, edit club/course dialogs, add course to club | Mobile |
| Player Merge | ✅ Done | Already implemented — merge request/accept/decline in PlayersPage | Mobile |
| Advanced Analytics | ✅ Done | Already implemented — course/holes/type filters, comparison, par breakdown | Mobile |
| Add/Edit Players | ✅ Done | Already implemented — full CRUD in PlayersPage | Mobile |

### 12.3 Admin Area Enhancements

| Feature | Status | Description | Platform |
|---------|--------|-------------|----------|
| Admin Dashboard | ✅ Done | System overview — user/round/course counts, recent activity, quick links | Web |
| User Management | ✅ Done | View/search users, assign roles (promote/demote admin) | Web |
| Player Management | ✅ Done | View/search/edit all players, linked accounts, round counts | Web |
| Content Health | ✅ Done | Clubs without courses, hole count mismatches, par mismatches, stroke index duplicates | Web |
| Connection & Merge Oversight | ✅ Done | View all connections/merges with status filters | Web |
| System Notifications | ✅ Done | View all user notifications, type breakdown, read/unread stats | Web |
| Audit Trail | ✅ Done | AI audit log viewer with filters, expandable prompt/response detail | Web |
| Application Settings | ✅ Done | Key-value settings store — maintenance mode, registration toggle, AI limits, site name, configurable from admin UI with auto-seeded defaults | Web |
| System Health | ✅ Done | App uptime, memory usage, DB query time, database size, table row counts, 7-day activity stats, AI performance metrics, round status breakdown | Web |

### 12.4 New Features (Both Platforms)

| Feature | Description | Platform |
|---------|-------------|----------|
| Live Round Mode | ✅ Done — Real-time hole-by-hole scoring with auto-save after each hole, resume capability, running totals, full scorecard view | Both |
| Tee Sets & Course Ratings | ✅ Done — Multiple tee colours per course with per-tee par, stroke index, yardage, course rating and slope rating | Both |
| Golf Societies | ✅ Done — Society creation, membership, roles (events arrive with Phase 3 Competitions) | Both |
| Club & Society Membership | ✅ Done — Users self-register as members of clubs and societies, with future admin approval | Both |
| Competition Framework | Competition entities with scoring formats (Medal, Stableford, Match Play) linked to clubs/societies | Both |
| Handicap Tracking | Multi-source handicaps (personal, club/regional, society) with WHS calculation and history | Both |
| Goal Setting & Milestones | Set targets (break 90, improve par-3 average) with progress tracking | Both |
| Structured Weather Data | Replace free-text notes with temperature, wind, conditions fields | Both |
| Export & Share | PDF round cards, share stats on social media, CSV export | Both |

---

### 12.5 Tee Sets, Societies, Competitions & Handicaps — Implementation Plan

This is a large, interdependent set of features. The plan is split into 4 phases that must be delivered in order because each phase depends on the previous one.

#### Current State (What Exists Today)

| Entity | Key Fields | Notes |
|--------|-----------|-------|
| `Hole` | HoleNumber, Par, StrokeIndex, LengthYards | Single set of values per hole — no tee variants |
| `GolfCourse` | Name, DefaultPar, NumberOfHoles | No course rating or slope rating |
| `GolfClub` | Name, Address, Website | No membership concept |
| `Round` | GolfCourseId, DatePlayed, RoundType (Friendly/Competitive), Status | No competition link, no tee tracking |
| `RoundPlayer` | RoundId, PlayerId (composite PK) | No tee selection per player |
| `Score` | RoundId, PlayerId, HoleId, Strokes, Putts, FairwayHit | Scores always reference default hole par |
| `Player` | Handicap (single double?) | Single handicap value, no source or history |
| Holes.csv | ClubName, CourseName, HoleNumber, Par, StrokeIndex, LengthYards | All yardages are default (Yellow) tees |

---

#### Phase 1: Tee Sets & Course Ratings ✅ Done

**Goal**: Support multiple tee colours per course, with per-tee hole data. Track which tees each player plays from in every round.

##### 1.1 New Models

```
TeeSet
├── TeeSetId (PK)
├── GolfCourseId (FK → GolfCourse)
├── Name (string, e.g. "Yellow", "White", "Red", "Blue")
├── Colour (string, hex or named colour for UI)
├── CourseRating (decimal?, e.g. 71.2)
├── SlopeRating (int?, e.g. 128)
├── TotalYardage (int?, computed or stored)
├── Gender (enum: Unisex/Male/Female — some tees are gender-specific)
├── SortOrder (int — display ordering)
└── Navigation: GolfCourse, HoleTees[]

HoleTee
├── HoleTeeId (PK)
├── HoleId (FK → Hole)
├── TeeSetId (FK → TeeSet)
├── Par (int)
├── StrokeIndex (int?)
├── LengthYards (int?)
└── Navigation: Hole, TeeSet
```

##### 1.2 Schema Changes to Existing Models

| Model | Change | Reason |
|-------|--------|--------|
| `Hole` | Keep Par, StrokeIndex, LengthYards as "default tee" values | Backwards compatibility — existing scores still reference hole.Par |
| `GolfCourse` | Add `DbSet<TeeSet>` navigation | Course owns its tee sets |
| `RoundPlayer` | Add `TeeSetId (int?, FK → TeeSet)` | Track which tees each player plays from |
| `Score` | Add `TeeSetId (int?, FK → TeeSet)` | Denormalised for efficient scorecard queries and handicap calculation; populated from scorecard entries when saved |

##### 1.3 Data Migration Strategy

1. **Add TeeSet + HoleTee tables** via EF migration
2. **Seed a "Yellow" TeeSet** for every existing GolfCourse
3. **Copy existing Hole data** into HoleTee rows: `Hole.Par → HoleTee.Par`, `Hole.StrokeIndex → HoleTee.StrokeIndex`, `Hole.LengthYards → HoleTee.LengthYards`
4. **Existing Hole columns remain** — they serve as the "default" view and keep all existing queries, scorecards, and reports working
5. **RoundPlayer.TeeSetId** defaults to NULL for all historical rounds — null means "used default/Yellow tees"
6. **No data loss, no breaking changes to existing queries**

##### 1.4 CSV Import Updates

**Holes.csv — New format** (backwards compatible):
```
ClubName,CourseName,HoleNumber,Par,StrokeIndex,LengthYards,TeeName
Stockwood Park Golf Centre,Academy,1,3,3,76,Yellow
Stockwood Park Golf Centre,Academy,1,3,3,82,White
```
- If `TeeName` column is missing → import as default hole data (current behaviour)
- If `TeeName` is present → create TeeSet if needed, create HoleTee row
- DataMigration.razor updated to handle both formats

**New optional CSV: TeeSets.csv**
```
ClubName,CourseName,TeeName,Colour,CourseRating,SlopeRating,Gender,SortOrder
Stockwood Park Golf Centre,Main Course,Yellow,#FFD700,68.5,121,Male,1
Stockwood Park Golf Centre,Main Course,Red,#FF0000,70.2,125,Female,2
```

##### 1.5 UI Changes

**Round Recording (Web + Mobile) — Setup phase:**
- After player selection → new "Tee Selection" step
- Dropdown per player showing available tee sets for the selected course
- Default selection: first tee set (Yellow if available)
- Players can play from different tees (adult Yellow, child Red)

**Live Round — Playing phase:**
- Hole card shows the correct par, stroke index and yardage for each player's tee
- Running total vs-par calculated against player-specific par
- Scorecard view shows tee colour badge next to each player name

**Scorecard / Round Detail views:**
- Show tee played next to player name
- Par row reflects the tee played (if players on different tees, show per-player)

**Course Detail page:**
- Show tee set tabs/selector
- Display hole table with per-tee data columns

**Admin Content Health:**
- Flag courses with no tee sets
- Flag tee sets with missing hole data

##### 1.6 API Changes

| Endpoint | Change |
|----------|--------|
| `GET /api/golfcourses/{id}` | Include `teeSets[]` with nested `holeTees[]` |
| `POST /api/rounds` (CreateRoundRequest) | Add `playerTeeSelections: [{playerId, teeSetId}]` |
| `PUT /api/rounds/{id}/scores/hole` | Add optional `teeSetId` per score |
| Mobile `GolfCourse` model | Add `TeeSets` collection |
| Mobile `RoundResponse` | Add tee info per player |

##### 1.7 Files Affected

| Layer | Files |
|-------|-------|
| Models | `TeeSet.cs` (new), `HoleTee.cs` (new), `RoundPlayer.cs`, `Score.cs`, `GolfCourse.cs` |
| Data | `ApplicationDbContext.cs`, new migration, `SeedData.cs`, CSV files |
| Services | `IHoleService`, `HoleService`, `IGolfCourseService`, `GolfCourseService`, `IRoundService`, `RoundService`, `IScoreService`, `ScoreService` |
| Controllers | `GolfCoursesController`, `RoundsController` |
| Web Pages | `RecordRound.razor`, `LiveRound.razor`, `RoundDetails.razor`, Course detail page, `ContentHealth.razor`, `DataMigration.razor` |
| Mobile | `GolfCourse.cs` (model), `RoundApiService.cs`, `RecordRoundPage.razor`, `LiveRoundPage.razor`, `RoundDetailPage.razor` |
| Docs | `ARCHITECTURE.md` |

---

#### Phase 2: Golf Societies & Memberships ✅ Done

**Goal**: Users can create and join golf societies. Users can also register as members of golf clubs. Both concepts support future admin roles.

##### 2.1 New Models

```
GolfSociety
├── GolfSocietyId (PK)
├── Name (string, required)
├── Description (string?)
├── CreatedByUserId (FK → ApplicationUser)
├── CreatedAt (DateTime)
├── IsActive (bool)
└── Navigation: Members[], Events[]

SocietyMembership
├── SocietyMembershipId (PK)
├── GolfSocietyId (FK → GolfSociety)
├── ApplicationUserId (FK → ApplicationUser)
├── Role (enum: Member, Admin, Owner)
├── JoinedAt (DateTime)
├── IsActive (bool)
└── Unique: (GolfSocietyId, ApplicationUserId)

ClubMembership
├── ClubMembershipId (PK)
├── GolfClubId (FK → GolfClub)
├── ApplicationUserId (FK → ApplicationUser)
├── Role (enum: Member, Admin)
├── MembershipNumber (string?, official club number)
├── JoinedAt (DateTime)
├── IsActive (bool)
└── Unique: (GolfClubId, ApplicationUserId)
```

##### 2.2 Schema Changes to Existing Models

| Model | Change | Reason |
|-------|--------|--------|
| `GolfClub` | Add `Memberships` navigation | Club has members |
| `ApplicationUser` | Add `ClubMemberships` + `SocietyMemberships` navigations | User can be member of many clubs and societies |

##### 2.3 Features

**Society Management (Web + Mobile):**
- Create society (name, description)
- Browse/search societies
- Join a society (self-registration, immediate)
- View "My Societies" list
- Society detail page — member list, recent rounds by members

**Club Membership (Web + Mobile):**
- "Join Club" from club detail page
- Optional membership number
- View "My Clubs" list
- Club detail page shows member count

**Admin (Web only):**
- Admin overview: total societies, memberships
- Future: approve/manage society and club admins

##### 2.4 Files Affected

| Layer | Files |
|-------|-------|
| Models | `GolfSociety.cs` (new), `SocietyMembership.cs` (new), `ClubMembership.cs` (new), `MembershipRole.cs` (new enum), `GolfClub.cs` |
| Data | `ApplicationDbContext.cs`, `ApplicationUser.cs`, new migration |
| Services | `IGolfSocietyService` (new), `IClubMembershipService` (new) |
| Controllers | `SocietiesController` (new), `ClubMembershipsController` (new) |
| Web Pages | `Societies/` folder (new: List, Detail, Create), `GolfClubs/` (add Join button), `Account/Manage` (My Clubs, My Societies) |
| Mobile | Society models, API services, pages (list, detail, join), club join UI |
| Nav | Add "Societies" link to both web nav and mobile nav |

---

#### Phase 3: Competitions & Scoring Formats

**Goal**: Clubs and societies can create competitions. Rounds can be linked to competitions. Support multiple scoring formats.

##### 3.1 New Models

```
ScoringFormat (enum)
├── Medal (Stroke Play)
├── Stableford
├── ModifiedStableford
├── MatchPlay
├── BetterBall
├── Scramble
├── TexasScramble
├── Fourball
├── Foursomes
├── Bogey

Competition
├── CompetitionId (PK)
├── Name (string, required, e.g. "Monthly Medal March 2026")
├── GolfClubId (FK → GolfClub, nullable)
├── GolfSocietyId (FK → GolfSociety, nullable)
├── GolfCourseId (FK → GolfCourse, nullable — where it's played)
├── ScoringFormat (enum)
├── Date (DateTime)
├── Description (string?)
├── IsOpen (bool — can anyone join, or members only)
├── Status (enum: Upcoming, InProgress, Completed, Cancelled)
├── CreatedByUserId (FK)
├── CreatedAt (DateTime)
└── Navigation: Rounds[], Entries[]

CompetitionEntry
├── CompetitionEntryId (PK)
├── CompetitionId (FK → Competition)
├── PlayerId (FK → Player)
├── TeeSetId (FK → TeeSet, nullable)
├── HandicapAtEntry (decimal? — snapshot of handicap used)
├── GrossScore (int?)
├── NetScore (int?)
├── StablefordPoints (int?)
├── Position (int?)
└── Navigation: Competition, Player, TeeSet
```

##### 3.2 Schema Changes to Existing Models

| Model | Change | Reason |
|-------|--------|--------|
| `Round` | Add `CompetitionId (int?, FK → Competition)` | Link round to competition |
| `RoundTypeOption` | Expand: `Casual, ClubCompetition, SocietyEvent, OpenCompetition, FriendlyMatch` | Richer round context (backwards-compatible: map existing Friendly→Casual, Competitive→ClubCompetition) |

##### 3.3 Features

**Competition Management (Web + Mobile):**
- Create competition (from club or society context)
- Set scoring format, course, date
- Enter/register for competition
- Link a recorded round to a competition
- Auto-calculate results based on scoring format

**Scoring Format Logic:**
- **Medal**: Gross strokes, net = gross - handicap
- **Stableford**: Points per hole based on par and handicap strokes received
- **Match Play**: Hole-by-hole win/loss/halve tracking

**Results & Leaderboards:**
- Competition results table (position, player, gross, net, points)
- History of competitions per club/society

##### 3.4 Files Affected

| Layer | Files |
|-------|-------|
| Models | `Competition.cs` (new), `CompetitionEntry.cs` (new), `ScoringFormat.cs` (new enum), `Round.cs` |
| Data | `ApplicationDbContext.cs`, new migration |
| Services | `ICompetitionService` (new), `IScoringService` (new — scoring format calculations) |
| Controllers | `CompetitionsController` (new) |
| Web Pages | `Competitions/` folder (new: List, Detail, Create, Results), link from Club & Society pages |
| Mobile | Competition models, API services, pages |
| Round Recording | Add competition selector in setup, link round on save |

---

#### Phase 4: Multi-Source Handicap Tracking

**Goal**: Track handicaps from three sources (personal, club/regional, society), maintain history, auto-calculate personal handicap using WHS principles.

**Delivery increments** (Phase 4 is decoupled from Phase 3 — see revised dependency chain):

| Increment | Scope | Depends on |
|-----------|-------|-----------|
| **4a** | `ScoringDifferential` + personal WHS index, recalculated on round completion (hook in `RoundService` — all web/mobile/live completion paths converge there), plus an idempotent admin backfill over historical rounds | Tee sets with rating/slope (✅ exist) |
| **4b** | Manual club/regional handicap entry with history, handicap dashboard UI (web + mobile), primary-handicap selector | 4a models |
| **4c** | Society handicaps (same engine filtered to rounds linked to a society's competitions) | Phase 3 (Competitions) |

##### 4.1 New Models

```
HandicapSource (enum)
├── Personal       — auto-calculated from all qualifying rounds
├── ClubRegional   — official handicap from club/national body (manually entered or synced)
├── Society        — calculated from society competition rounds

HandicapRecord
├── HandicapRecordId (PK)
├── PlayerId (FK → Player)
├── HandicapIndex (decimal, e.g. 18.4)
├── Source (HandicapSource enum)
├── GolfSocietyId (FK → GolfSociety, nullable — only for Society source)
├── GolfClubId (FK → GolfClub, nullable — only for ClubRegional source)
├── EffectiveDate (DateTime)
├── ExpiryDate (DateTime? — for club handicaps with renewal)
├── CalculationDetails (string? — JSON: which rounds, differentials, etc.)
├── IsManualEntry (bool — true for club handicaps entered by user)
├── CreatedAt (DateTime)
└── Navigation: Player, GolfSociety?, GolfClub?

ScoringDifferential
├── ScoringDifferentialId (PK)
├── PlayerId (FK → Player)
├── RoundId (FK → Round)
├── TeeSetId (FK → TeeSet)
├── AdjustedGrossScore (int — after max score adjustments)
├── CourseRating (decimal)
├── SlopeRating (int)
├── Differential (decimal — the calculated value)
├── IsUsedInCalculation (bool — is this in the best 8 of 20?)
├── CalculatedAt (DateTime)
└── Navigation: Player, Round, TeeSet
```

##### 4.2 Schema Changes to Existing Models

| Model | Change | Reason |
|-------|--------|--------|
| `Player` | Keep `Handicap` field as display/convenience | Shows the "active" handicap (user chooses which source is primary) |
| `Player` | Add `PrimaryHandicapSource (HandicapSource?)` | Which handicap context is shown as "my handicap" |
| `Player` | Add `HandicapRecords` navigation | History |

##### 4.3 WHS Calculation Logic (Personal Handicap)

1. After each qualifying round (completed, 18 holes, course has tee set with rating/slope):
   - Calculate Score Differential: `(113 / SlopeRating) × (AdjustedGrossScore - CourseRating)`, rounded to 1 decimal
   - **Adjusted Gross Score, v1 simplification**: cap each hole at `par + 5` (the WHS rule for players without an established index). v2 (later): net double bogey using the player's index at round date and `Hole.StrokeIndex`.
   - Store as `ScoringDifferential` record
2. Handicap Index from the last 20 differentials — full WHS table:

   | Differentials available | Calculation | Adjustment |
   |------------------------|-------------|------------|
   | 3 | Lowest 1 | −2.0 |
   | 4 | Lowest 1 | −1.0 |
   | 5 | Lowest 1 | — |
   | 6 | Average of lowest 2 | −1.0 |
   | 7–8 | Average of lowest 2 | — |
   | 9–11 | Average of lowest 3 | — |
   | 12–14 | Average of lowest 4 | — |
   | 15–16 | Average of lowest 5 | — |
   | 17–18 | Average of lowest 6 | — |
   | 19 | Average of lowest 7 | — |
   | 20 | Average of lowest 8 | — |

3. Recalculate after every qualifying round (trigger: status transition to `Completed` inside `RoundService`)
4. Store new `HandicapRecord` with source=Personal (only when the index changed)

Steps 1–2 are implemented as pure functions in `GolfTrackerApp.Core/Services/WhsCalculator.cs` (`ComputeAdjustedGrossScore`, `ComputeDifferential`, `ComputeIndex` — index capped at 54.0, plus handicaps negative, half-away-from-zero rounding). Steps 3–4 (persistence + trigger) are WORKLOG 2-1/2-3.

**Society Handicap**: Same calculation but only using rounds linked to that society's competitions.

**Club/Regional Handicap**: Manually entered by user (or imported). Updated when user receives new official handicap from their club.

##### 4.4 Features

**Handicap Dashboard (Web + Mobile):**
- Show all active handicaps: Personal, Club (per club membership), Society (per society)
- Handicap history chart over time
- Scoring differentials table (last 20 rounds)
- Which differentials are "counting" in the calculation

**Round Completion Flow:**
- After completing a round: auto-calculate scoring differential if tee set has rating/slope
- Recalculate personal handicap index
- If round is linked to a society competition: recalculate society handicap too
- Notify user if handicap changed

**Player Profile:**
- Show handicap badges for each context
- "Primary handicap" selector

**Reports:**
- Handicap progression over time
- Handicap comparison across contexts
- Best/worst differentials

##### 4.5 Files Affected

| Layer | Files |
|-------|-------|
| Models | `HandicapRecord.cs` (new), `ScoringDifferential.cs` (new), `HandicapSource.cs` (new enum), `Player.cs` |
| Data | `ApplicationDbContext.cs`, new migration |
| Services | `IHandicapService` (new — calculation engine), `IHandicapHistoryService` (new) |
| Controllers | `HandicapsController` (new) |
| Web Pages | `Handicaps/` folder (new: Dashboard, History), Player profile enhancements |
| Mobile | Handicap models, API services, pages |
| Round Flow | Post-round handicap recalculation trigger |

---

#### Dependency Chain & Build Order

```
Phase 1: Tee Sets ✅        Phase 2: Societies ✅
    │      │                    │
    │      └────────┐    ┌──────┘
    │               │    │
    ▼               ▼    ▼
Phase 4a/4b:    Phase 3: Competitions
Personal +           │
club handicaps       ▼
                Phase 4c: Society handicaps
```

- **Phases 1 and 2 are complete** ✅
- **Phase 3 requires both** — competitions need tee sets (for handicap strokes) and societies/clubs (as hosts)
- **Phase 4a/4b do NOT require Phase 3** — personal differentials only need tee sets with rating/slope (which exist); club handicaps are manually entered
- **Only Phase 4c (society handicaps) requires Phase 3** — it filters differentials to rounds linked to a society's competitions

**Recommended build order**: Phase 4a → 4b (top priority, delivers handicaps) → Phase 3 → Phase 4c.

---

#### Migration Safety Rules

1. **All new columns are nullable or have defaults** — no breaking changes to existing data
2. **Existing queries continue to work** — Hole.Par/StrokeIndex/LengthYards remain as the default view
3. **Historical rounds don't need tee data** — `RoundPlayer.TeeSetId = null` means "default tees"
4. **Enum expansions are additive** — `RoundTypeOption.Friendly` stays at value 0
5. **CSV import is backwards compatible** — old format still works, new columns are optional
6. **Each phase gets its own EF migration** — can be rolled back independently

## 13. Future Architecture Evolution

The current architecture is designed for easy evolution:

1. **Dedicated API project**: Move `Controllers/` to `GolfTrackerApp.Api`, reference `GolfTrackerApp.Core`
2. **Independent deployment**: Web and API can scale independently
3. **Additional clients**: Any platform (React, Flutter, etc.) can consume the same API

The interface-driven service layer keeps these refactors mechanical — no business logic changes required.
