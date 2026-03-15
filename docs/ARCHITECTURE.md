# Golf Tracker App — System Architecture

## 1. Overview

Golf Tracker App is a cross-platform golf performance tracking system consisting of two front-end applications — a Blazor Server web app and a .NET MAUI Blazor Hybrid mobile app — sharing a centralised API backend and business logic layer hosted within the web project.

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

## 2. Design Principles

### 2.1 Centralised Business Logic
All business logic lives in the Web project's **Service Layer**. Both the web Blazor components and the API controllers consume the same services via dependency injection. This guarantees:
- Identical behaviour on web and mobile
- A single place to fix bugs or add features
- No risk of logic drift between platforms

### 2.2 API-First for Mobile
The mobile app communicates exclusively through REST API endpoints. Every feature available on mobile has a corresponding API controller action. This creates a clean contract between client and server.

### 2.3 Future-Proof API Separation
The current architecture intentionally hosts API controllers within the Web project for MVP simplicity. The service layer is fully interface-driven (`IGolfClubService`, `IRoundService`, etc.), making it straightforward to:
1. Extract services into a shared class library
2. Move API controllers into a dedicated API project
3. Deploy web and API independently

This refactor requires no changes to business logic — only DI registration and project references.

## 3. Component Architecture

### 3.1 Web Project (GolfTrackerApp.Web)

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
├── Services/                           # Business logic (interfaces + implementations)
│   ├── IGolfClubService.cs            # Golf club operations
│   ├── IGolfCourseService.cs          # Golf course operations
│   ├── IRoundService.cs               # Round CRUD + queries
│   ├── IPlayerService.cs             # Player CRUD + search
│   ├── IReportService.cs             # Performance stats, comparisons, distributions
│   ├── IScoreService.cs              # Score CRUD + scorecard save
│   ├── IHoleService.cs               # Hole CRUD
│   ├── IConnectionService.cs         # Player-to-player social connections
│   ├── IMergeService.cs              # Managed player merge workflow
│   ├── INotificationService.cs       # User notifications
│   ├── IRoundWorkflowService.cs      # Multi-step round recording orchestrator
│   ├── IAiInsightService.cs          # Golf-specific AI insight orchestration
│   ├── IAiRoutingService.cs          # Multi-provider routing + failover
│   ├── IAiAuditService.cs            # AI audit logging + rate limiting
│   ├── IAiChatService.cs             # Persistent chat session management
│   ├── IAiProviderSettingsService.cs  # Admin provider on/off + priority
│   └── AiProviders/                   # AI provider implementations
│       ├── OpenAiProviderService.cs
│       ├── AnthropicProviderService.cs
│       ├── GeminiProviderService.cs
│       ├── GrokProviderService.cs
│       ├── MistralProviderService.cs
│       ├── DeepSeekProviderService.cs
│       ├── MetaLlamaProviderService.cs
│       └── ManusProviderService.cs
├── Models/                            # Domain models (EF entities + DTOs)
│   ├── Round.cs, Score.cs, Player.cs  # Core entities
│   ├── GolfClub.cs, GolfCourse.cs, Hole.cs
│   ├── PlayerConnection.cs           # Social connections
│   ├── PlayerMergeRequest.cs         # Merge workflow
│   ├── Notification.cs               # In-app notifications
│   ├── AiProviderConfig.cs           # AI provider configuration model
│   ├── AiProviderSettings.cs         # DB entity for provider on/off + priority
│   ├── AiInsightResult.cs            # AI response DTO
│   ├── AiChatMessage.cs              # Chat message DTO + AiChatRequest
│   ├── AiAuditLog.cs                 # Audit log entity
│   ├── AiChatSession.cs              # Chat session entity
│   ├── AiChatSessionMessage.cs       # Chat message entity
│   └── [Various DTOs]                # Report view models, chart data, etc.
├── Data/
│   ├── ApplicationDbContext.cs        # EF Core context (Identity + domain entities)
│   ├── ApplicationUser.cs            # Identity user (extends IdentityUser with LinkedPlayerId, AiInsightsOptOut)
│   ├── SeedData.cs                   # Initial data seeding
│   └── Migrations/                   # EF Core migrations (SQLite dev)
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

### 3.2 Mobile Project (GolfTrackerApp.Mobile)

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
| **Schema management** | EF Core Migrations (`context.Database.Migrate()`) | `EnsureCreated()` + manual SQL in `EnsureNewTablesExistAsync()` |
| **Column types** | `INTEGER`, `TEXT`, `REAL` | `INT`, `NVARCHAR(n)`, `DATETIME2`, `BIT`, etc. |
| **Cascade deletes** | Generally permissive | Strict — rejects `ON DELETE SET NULL` / `CASCADE` if it creates multiple cascade paths |
| **Config key** | `"DatabaseProvider": "Sqlite"` (in `appsettings.Development.json`) | `"DatabaseProvider": "SqlServer"` (in `appsettings.Production.json`) |

**When making any database schema change, you MUST:**

1. **Create an EF Core migration** for SQLite/development:
   ```bash
   cd GolfTrackerApp.Web
   dotnet ef migrations add <MigrationName>
   ```

2. **Update `EnsureNewTablesExistAsync()` in `Program.cs`** for SQL Server/production:
   - New tables: Add a `TableExistsAsync` check and `CREATE TABLE` with SQL Server types
   - New columns on existing tables: Add a `ColumnExistsAsync` check and `ALTER TABLE ... ADD`
   - Use `NVARCHAR(n)` not `TEXT`, `INT` not `INTEGER`, `DATETIME2` not `TEXT`, `BIT` not `INTEGER`

3. **Avoid cascade conflicts on SQL Server:**
   - Use `ON DELETE NO ACTION` for foreign keys where multiple cascade paths exist (e.g., `AspNetUsers` ↔ `Players`)
   - `ON DELETE CASCADE` is only safe when there's a single path from parent to dependent
   - `ON DELETE SET NULL` also triggers the cascade-path check on SQL Server

4. **Test both providers** before deploying schema changes.

## 6. Service Layer Design

All services follow the same pattern:
- **Interface** in `Services/I{Name}Service.cs`
- **Implementation** in `Services/{Name}Service.cs`
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
| Application Settings | Planned | Feature flags (maintenance mode, registration), configurable from UI | Web |
| System Health | Planned | API response times, error rates, database size, background job status | Web |

### 12.4 New Features (Both Platforms)

| Feature | Description | Platform |
|---------|-------------|----------|
| Live Round Mode | Real-time scoring during play with auto-save (placeholder exists in web) | Both |
| Handicap Tracking | Formal handicap index calculation and history over time | Both |
| Goal Setting & Milestones | Set targets (break 90, improve par-3 average) with progress tracking | Both |
| Structured Weather Data | Replace free-text notes with temperature, wind, conditions fields | Both |
| Tee Selection & Course Rating | Track which tees were played for accurate handicap calculations | Both |
| Export & Share | PDF round cards, share stats on social media, CSV export | Both |

## 13. Future Architecture Evolution

The current architecture is designed for easy evolution:

1. **Extract shared library**: Move `Models/` and `Services/` interfaces to a `GolfTrackerApp.Shared` class library
2. **Dedicated API project**: Move `Controllers/` to `GolfTrackerApp.Api`, reference the shared library
3. **Independent deployment**: Web and API can scale independently
4. **Additional clients**: Any platform (React, Flutter, etc.) can consume the same API

The interface-driven service layer ensures this refactor is mechanical — no business logic changes required.
