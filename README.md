# Golf Tracker App

A comprehensive web-based golf performance tracking application built with Blazor Server and .NET 9.

## 🏌️ Overview

Golf Tracker App allows golfers to track their rounds, analyze performance, and manage golf courses and clubs. The application provides both public access for browsing golf facilities and authenticated access for comprehensive performance tracking.

## ✨ Key Features

### 🌐 Public Access
- **Golf Club Directory**: Browse golf clubs and courses without registration
- **Course Information**: View detailed hole-by-hole course information
- **Public Landing Page**: Professional showcase encouraging user registration

### 🏆 Authenticated Features
- **Performance Dashboard**: Comprehensive golf statistics and analytics
- **Round Recording**: Easy-to-use scorecard interface
- **Performance Tracking**: Charts, trends, and improvement analysis
- **Course Management**: Add and manage golf courses and clubs
- **Playing Partner Records**: Track competitive records with friends

## 🏗️ Architecture

- **Frontend**: Blazor Server with MudBlazor UI components
- **Backend**: ASP.NET Core with Entity Framework
- **Database**: SQLite (development), easily configurable for other providers
- **Authentication**: ASP.NET Core Identity
- **Styling**: Centralized CSS architecture with component-based styling

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2024 or VS Code

### Running the Application
```bash
cd GolfTrackerApp.Web
dotnet run
```

The application will start at `https://localhost:5001` (or check console output for exact URL).

### Default Credentials
- **Admin User**: admin@golftracker.local
- **Password**: AdminPa$$w0rd!

## 📁 Project Structure

```
GolfTrackerApp/
├── docs/                           # Documentation
│   ├── architecture/              # Technical architecture docs
│   └── features/                  # Feature implementation guides
├── GolfTrackerApp.Web/           # Main web application
│   ├── Components/               # Blazor components
│   ├── Data/                    # Entity Framework context and models
│   ├── Models/                  # Domain models
│   ├── Services/                # Business logic services
│   └── wwwroot/                 # Static assets and CSS
└── README.md                    # This file
```

## 🎨 UI Framework

The application uses a centralized CSS architecture:
- **MudBlazor**: Primary UI component library
- **Custom Components**: Golf-specific styled components
- **Responsive Design**: Mobile-first approach
- **Theme**: Golf-inspired color scheme with modern glass-morphism effects

## 🔐 Security

- **Public Pages**: Golf clubs and courses browsable without authentication
- **Protected Features**: Performance tracking and course management require authentication
- **Role-based Access**: Admin and User roles for different permission levels

## 📖 Documentation

Detailed documentation is available in the `/docs` directory:

- **[CSS Architecture](docs/architecture/css-architecture.md)**: Centralized styling approach
- **[Public Access Features](docs/features/public-access.md)**: Implementation of public browsing
- **[Dashboard Features](docs/features/dashboard.md)**: Performance tracking capabilities

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🏌️‍♂️ About

Golf Tracker App is designed to help golfers of all skill levels track their performance, discover new courses, and improve their game through detailed analytics and performance insights.
