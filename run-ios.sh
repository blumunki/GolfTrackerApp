#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🍎 Golf Tracker iOS Runner${NC}"
echo "=================================="

# Function to check if Xcode tools are available
check_ios_tools() {
    if ! command -v xcrun &> /dev/null; then
        echo -e "${RED}❌ Error: xcrun not found. Please install Xcode Command Line Tools.${NC}"
        exit 1
    fi

    if ! command -v xcodebuild &> /dev/null; then
        echo -e "${RED}❌ Error: xcodebuild not found. Please install Xcode.${NC}"
        exit 1
    fi
}

# Function to find the best available iPhone simulator
find_simulator() {
    # Look for available iPhone simulators (prefer iPhone 16, then latest available)
    local sim_udid=""
    local sim_name=""

    # Try iPhone 16 first, then 15, then any iPhone
    for model in "iPhone 16" "iPhone 15" "iPhone"; do
        local match=$(xcrun simctl list devices available -j 2>/dev/null | \
            python3 -c "
import json, sys
data = json.load(sys.stdin)
for runtime, devices in data.get('devices', {}).items():
    if 'iOS' not in runtime:
        continue
    for d in devices:
        if d.get('isAvailable', False) and '${model}' in d.get('name', ''):
            print(d['udid'] + '|' + d['name'])
            sys.exit(0)
" 2>/dev/null)

        if [ -n "$match" ]; then
            sim_udid=$(echo "$match" | cut -d'|' -f1)
            sim_name=$(echo "$match" | cut -d'|' -f2)
            echo "$sim_udid|$sim_name"
            return 0
        fi
    done

    echo ""
    return 1
}

# Function to check if a simulator is booted
check_simulator_running() {
    local sim_udid="$1"
    local state=$(xcrun simctl list devices -j 2>/dev/null | \
        python3 -c "
import json, sys
data = json.load(sys.stdin)
for runtime, devices in data.get('devices', {}).items():
    for d in devices:
        if d.get('udid') == '${sim_udid}':
            print(d.get('state', 'Unknown'))
            sys.exit(0)
print('Unknown')
" 2>/dev/null)
    echo "$state"
}

# Function to start a simulator
start_simulator() {
    local sim_udid="$1"
    local sim_name="$2"

    echo -e "${YELLOW}📱 No iOS simulator running. Starting one...${NC}"
    echo -e "${BLUE}🚀 Booting simulator: $sim_name${NC}"

    xcrun simctl boot "$sim_udid" 2>/dev/null

    echo -e "${YELLOW}⏳ Waiting for simulator to boot up...${NC}"

    # Wait for simulator to be ready (timeout after 2 minutes)
    local timeout=120
    local elapsed=0
    while [ $elapsed -lt $timeout ]; do
        local state=$(check_simulator_running "$sim_udid")
        if [ "$state" = "Booted" ]; then
            echo -e "${GREEN}✅ Simulator is ready!${NC}"
            # Open the Simulator app
            open -a Simulator
            sleep 3
            return 0
        fi
        sleep 5
        elapsed=$((elapsed + 5))
        echo -e "${BLUE}⏳ Still waiting... ($elapsed/${timeout}s)${NC}"
    done

    echo -e "${RED}❌ Timeout waiting for simulator to start${NC}"
    exit 1
}

# Function to build and deploy the app
build_and_deploy() {
    local sim_udid="$1"
    local sim_name="$2"

    echo -e "${BLUE}🔨 Building Golf Tracker Mobile app...${NC}"

    cd GolfTrackerApp.Mobile

    # Build for iOS simulator
    echo -e "${YELLOW}📦 Building for iOS...${NC}"
    dotnet build -f net10.0-ios

    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Build failed!${NC}"
        exit 1
    fi

    echo -e "${GREEN}✅ Build successful!${NC}"

    # Install app on simulator
    echo -e "${YELLOW}📦 Installing app on simulator...${NC}"
    xcrun simctl install "$sim_udid" "bin/Debug/net10.0-ios/iossimulator-arm64/GolfTrackerApp.Mobile.app"

    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ App installation failed!${NC}"
        exit 1
    fi

    # Launch the app
    echo -e "${YELLOW}🚀 Launching Golf Tracker app...${NC}"
    xcrun simctl launch "$sim_udid" com.golftracker.mobile

    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ App launch failed!${NC}"
        exit 1
    fi

    echo -e "${GREEN}✅ App deployed and launched successfully!${NC}"
    cd ..
}

# Function to start logging
start_logging() {
    local sim_udid="$1"

    echo -e "${BLUE}📋 Starting iOS logging...${NC}"
    echo -e "${YELLOW}💡 Look for authentication, HTTP, and error messages${NC}"
    echo -e "${YELLOW}💡 Press Ctrl+C to stop logging${NC}"
    echo "=================================="

    # Stream simulator logs and filter for relevant messages
    xcrun simctl spawn "$sim_udid" log stream --predicate 'subsystem CONTAINS "com.golftracker" OR composedMessage CONTAINS[cd] "GolfTracker" OR composedMessage CONTAINS[cd] "MAUI" OR composedMessage CONTAINS[cd] "Blazor" OR composedMessage CONTAINS[cd] "Authentication" OR composedMessage CONTAINS[cd] "Exception" OR composedMessage CONTAINS[cd] "Error"' --level debug 2>/dev/null || \
    echo -e "${YELLOW}💡 Log streaming ended. You can also check Console.app for logs.${NC}"
}

# Main execution
echo -e "${BLUE}🔍 Checking iOS development environment...${NC}"
check_ios_tools

echo -e "${BLUE}📱 Finding available iOS simulator...${NC}"
sim_info=$(find_simulator)

if [ -z "$sim_info" ]; then
    echo -e "${RED}❌ No available iPhone simulators found.${NC}"
    echo -e "${YELLOW}💡 Please create a simulator using Xcode or:${NC}"
    echo -e "${YELLOW}   xcrun simctl create \"iPhone 16\" com.apple.CoreSimulator.SimDeviceType.iPhone-16${NC}"
    exit 1
fi

sim_udid=$(echo "$sim_info" | cut -d'|' -f1)
sim_name=$(echo "$sim_info" | cut -d'|' -f2)
echo -e "${GREEN}✅ Found simulator: $sim_name ($sim_udid)${NC}"

# Check if simulator is running
sim_state=$(check_simulator_running "$sim_udid")

if [ "$sim_state" != "Booted" ]; then
    start_simulator "$sim_udid" "$sim_name"
else
    echo -e "${GREEN}✅ Simulator '$sim_name' is already running${NC}"
fi

# Wait a moment for device to be fully ready
sleep 3

build_and_deploy "$sim_udid" "$sim_name"
start_logging "$sim_udid"
