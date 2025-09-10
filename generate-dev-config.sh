#!/bin/bash

# Generate Development Configuration Script
# This script generates a DevConfiguration.cs file from user secrets
# The generated file is excluded from git via .gitignore

cd "$(dirname "$0")/GolfTrackerApp.Mobile"

echo "🔧 Generating development configuration from user secrets..."

# Check if user secrets exist
if ! dotnet user-secrets list > /dev/null 2>&1; then
    echo "❌ User secrets not configured. Please run:"
    echo "   dotnet user-secrets set \"Authentication:Google:ClientId\" \"your-client-id\""
    echo "   dotnet user-secrets set \"Authentication:Google:ClientSecret\" \"your-client-secret\""
    exit 1
fi

# Extract values from user secrets
USER_SECRETS=$(dotnet user-secrets list)
CLIENT_ID=$(echo "$USER_SECRETS" | grep "Authentication:Google:ClientId" | cut -d'=' -f2- | xargs)
CLIENT_SECRET=$(echo "$USER_SECRETS" | grep "Authentication:Google:ClientSecret" | cut -d'=' -f2- | xargs)

if [ -z "$CLIENT_ID" ] || [ -z "$CLIENT_SECRET" ]; then
    echo "❌ Missing OAuth credentials in user secrets"
    exit 1
fi

# Generate the configuration class
cat > DevConfiguration.generated.cs << EOF
// This file is auto-generated from user secrets
// DO NOT EDIT MANUALLY - changes will be overwritten
// Generated on: $(date)

#if DEBUG
namespace GolfTrackerApp.Mobile.Generated
{
    internal static class DevConfiguration
    {
        public static readonly string GoogleClientId = "${CLIENT_ID}";
        public static readonly string GoogleClientSecret = "${CLIENT_SECRET}";
    }
}
#endif
EOF

echo "✅ Development configuration generated successfully"
echo "   File: DevConfiguration.generated.cs"
echo "   Client ID: ${CLIENT_ID:0:20}..."
echo "   Client Secret: [REDACTED]"
echo ""
echo "You can now build and run the mobile app!"
