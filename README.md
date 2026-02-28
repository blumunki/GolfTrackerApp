# Golf Tracker App

A cross-platform golf performance tracking application built with .NET 10, featuring a Blazor Server web app and a .NET MAUI Blazor Hybrid mobile app sharing a centralised API backend.

## Overview

Golf Tracker App allows golfers to record rounds, analyse performance trends, manage golf courses and clubs, connect with playing partners, and compare statistics — all from web or mobile. The architecture centralises business logic in a service layer exposed via REST APIs, ensuring consistent functionality across both platforms.

## Key Features

### Public Access (No Login Required)
- Browse a directory of golf clubs and courses
- View hole-by-hole course information (yardage, par, stroke index)
- Community statistics (total clubs, courses, countries)

### Authenticated Features
- **Performance Dashboard** — stats, score distribution, recent activity, favourite courses, playing partner records
- **Round Recording** — guided workflow: select club/course, add players, enter hole-by-hole scores
- **Player Management** — manage player profiles, invite connections, merge managed players into registered accounts
- **Player Reports** — per-player stats, scoring distribution, performance by par, head-to-head comparison
- **Golf Club & Course Management** — add/edit clubs and courses with hole details
- **Notifications** — connection requests, merge completions

### Mobile-Specific
- Google Sign-In authentication
- Native splash screen and app icon (golf-themed)
- Bottom navigation with page-based routing
- Full-screen stat cards and responsive layouts

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Web Frontend** | Blazor Server (.NET 10), MudBlazor 8.x |
| **Mobile Frontend** | .NET MAUI Blazor Hybrid (.NET 10), MudBlazor 7.x |
| **API Layer** | ASP.NET Core Web API controllers (hosted in Web project) |
| **Business Logic** | Service layer with interfaces (IGolfClubService, IRoundService, IReportService, etc.) |
| **Data Access** | Entity Framework Core 10 with IDbContextFactory |
| **Database** | SQLite (development) / SQL Server (production) |
| **Authentication** | ASP.NET Core Identity (web) + JWT Bearer tokens (API/mobile) + Google OAuth |
| **Styling** | Centralised CSS architecture with component-based files |

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- Xcode 26+ (for iOS builds)
- Android SDK (for Android builds)
- VS Code or Visual Studio 2025+

### Running the Web App
```bash
cd GolfTrackerApp.Web
dotnet run --launch-profile https
```
The app starts at `https://localhost:7298` (check console for exact URL).

### Running the Mobile App

**Android:**
```bash
./run-android.sh
```

**iOS:**
```bash
./run-ios.sh
```

Both scripts build the project, check for a running emulator/simulator, start one if needed, deploy, and stream logs.

### Development Configuration (Mobile)
Generate the mobile dev config to point at your local web server:
```bash
./generate-dev-config.sh
```
This creates `DevConfiguration.generated.cs` with the API base URL and OAuth credentials.

### Default Credentials
- **Admin**: admin@golftracker.local / AdminPa$$w0rd!

## Project Structure

```
GolfTrackerApp/
├── GolfTrackerApp.sln                  # Solution file
├── README.md                           # This file
├── run-android.sh                      # Android build + deploy script
├── run-ios.sh                          # iOS build + deploy script
├── generate-dev-config.sh              # Mobile dev config generator
│
├── docs/                               # Documentation
│   ├── ARCHITECTURE.md                 # System architecture document
│   ├── TODO.md                         # Improvement backlog
│   ├── architecture/                   # Technical docs
│   │   └── css-architecture.md
│   ├── features/                       # Feature docs
│   │   ├── dashboard.md
│   │   └── public-access.md
│   └── mobile-development-setup.md
│
├── GolfTrackerApp.Web/                 # Web application + API host
│   ├── Components/                     # Blazor components
│   │   ├── Pages/                      # Page components (Rounds, Players, GolfClubs, etc.)
│   │   ├── Layout/                     # MainLayout, NavMenu
│   │   ├── Shared/                     # Dialogs, shared components
│   │   └── Account/                    # Identity scaffolded pages
│   ├── Controllers/                    # REST API controllers
│   ├── Data/                           # EF Core DbContext, migrations, seed data
│   ├── Models/                         # Domain models (Round, Player, Score, etc.)
│   ├── Services/                       # Business logic services + interfaces
│   ├── Theme/                          # MudBlazor theme configuration
│   └── wwwroot/css/                    # Centralised CSS (components/, layout/, themes/, utilities/)
│
└── GolfTrackerApp.Mobile/              # MAUI Blazor Hybrid mobile app
    ├── Components/                     # Blazor components
    │   ├── Pages/                      # Mobile page components
    │   ├── App.razor                   # Root component with custom page routing
    │   ├── Dashboard/                  # Dashboard widget components
    │   └── Navigation/                 # Navigation components
    ├── Models/                         # Mobile-specific DTOs
    ├── Services/                       # Auth, config, navigation services
    │   └── Api/                        # API client services
    ├── Resources/                      # Icons, splash, fonts
    └── wwwroot/                        # Mobile static assets
```

## API Architecture

The web project hosts REST API controllers under `/api/*` that serve both:
1. **Web Blazor components** — call services directly (same process)
2. **Mobile app** — calls the API over HTTP with JWT authentication

Key API endpoints:
| Controller | Base Route | Purpose |
|-----------|-----------|---------|
| AuthController | `/api/auth` | Login, register, Google sign-in |
| DashboardController | `/api/dashboard` | Dashboard statistics |
| RoundsController | `/api/rounds` | Round CRUD |
| PlayersController | `/api/players` | Player CRUD, connections, merge, reports |
| GolfClubsController | `/api/golfclubs` | Golf club CRUD |
| GolfCoursesController | `/api/golfcourses` | Golf course CRUD |
| ReportsController | `/api/reports` | Aggregated reports |

## Authentication

- **Web**: ASP.NET Core Identity with cookie authentication + optional Google OAuth
- **Mobile**: Google Sign-In → JWT token issued by AuthController → stored on device
- **API**: JWT Bearer scheme (`ApiAuth`) on protected endpoints

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — system architecture, data flow, design decisions
- [Improvement Backlog](docs/TODO.md) — prioritised list of code optimisations and technical debt
- [CSS Architecture](docs/architecture/css-architecture.md) — styling conventions
- [Mobile Setup](docs/mobile-development-setup.md) — mobile development environment
- [Dashboard Features](docs/features/dashboard.md) — dashboard capabilities
- [Public Access](docs/features/public-access.md) — public browsing implementation

## License

This project is licensed under the MIT License.
