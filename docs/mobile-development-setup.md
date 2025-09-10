# MAUI Mobile Development Setup

## Problem
MAUI iOS apps run in a sandboxed environment and cannot access the host machine's user secrets directly. This means we can't use the standard ASP.NET Core user secrets approach that works for the web application.

## Solution
We use a code generation approach that creates a `DevConfiguration.generated.cs` file from user secrets. This file contains the OAuth credentials and is excluded from git.

## Setup Instructions

### 1. Configure User Secrets (One-time setup)
```bash
cd GolfTrackerApp.Mobile
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
```

### 2. Generate Development Configuration
```bash
./generate-dev-config.sh
```

This script:
- Extracts OAuth credentials from user secrets
- Generates `DevConfiguration.generated.cs` with the credentials
- The generated file is automatically excluded from git

### 3. Build and Run the Mobile App
```bash
dotnet build GolfTrackerApp.Mobile -f net9.0-ios -t:Run
```

## How It Works

1. **generate-dev-config.sh** extracts OAuth credentials from user secrets
2. **DevConfiguration.generated.cs** is created with the credentials as static constants
3. **MauiProgram.cs** loads these values into the configuration system at startup
4. **GoogleAuthenticationService** accesses them via dependency injection
5. **No secrets are stored in source code** ✅

## Security Benefits

- ✅ No secrets in source code
- ✅ No secrets in git repository  
- ✅ Each developer manages their own credentials
- ✅ Generated file is automatically excluded from git
- ✅ Build-time code generation ensures credentials are available at runtime
- ✅ Production builds can use different credential sources

## For Production

In production, you would:
1. Remove the `#if DEBUG` conditional compilation from the generated configuration
2. Use your deployment system's secret management to create `DevConfiguration.generated.cs`
3. Or implement a different configuration loading mechanism in `MauiProgram.cs`
4. Consider using Azure Key Vault, AWS Secrets Manager, or similar for credential storage

## Troubleshooting

If you see build errors about missing `DevConfiguration`:
1. Run `./generate-dev-config.sh` to generate the configuration file
2. Make sure you have user secrets configured (step 1 above)
3. Check that `DevConfiguration.generated.cs` exists in the `GolfTrackerApp.Mobile` directory

If authentication fails:
1. Verify your Google OAuth client configuration includes `http://localhost:7777/oauth/callback` as a redirect URI
2. Check that the Google OAuth client type is "Web application" (not "iOS" or "Android")
3. Ensure you're completing the OAuth flow in Safari without manually canceling
