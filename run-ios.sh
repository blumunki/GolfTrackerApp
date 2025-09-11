#!/bin/bash

# Golf Tracker App - iOS Deployment Script
# This script builds and deploys the MAUI iOS app to iPhone 16 simulator

echo "🏌️ Golf Tracker App - iOS Deployment"
echo "======================================"

# Navigate to the Mobile project directory
cd "$(dirname "$0")/GolfTrackerApp.Mobile"

echo "📱 Building iOS app..."
dotnet build -f net9.0-ios

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "✅ Build successful!"

# Check if iPhone 16 simulator is running
echo "📱 Checking simulator status..."
SIMULATOR_STATUS=$(xcrun simctl list devices | grep "iPhone 16" | grep "Booted")

if [ -z "$SIMULATOR_STATUS" ]; then
    echo "🔄 Booting iPhone 16 simulator..."
    xcrun simctl boot "iPhone 16"
    sleep 3  # Give simulator time to boot
else
    echo "✅ iPhone 16 simulator already running"
fi

echo "📦 Installing app on simulator..."
xcrun simctl install "iPhone 16" "bin/Debug/net9.0-ios/iossimulator-arm64/GolfTrackerApp.Mobile.app"

echo "🚀 Launching Golf Tracker app..."
xcrun simctl launch "iPhone 16" com.golftracker.mobile

echo "📱 Opening Simulator app..."
open -a Simulator

echo "🎉 Golf Tracker app is now running on iPhone 16 simulator!"
