#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🤖 Golf Tracker Android Runner${NC}"
echo "=================================="

# Function to check if Android SDK tools are available
check_android_tools() {
    if ! command -v adb &> /dev/null; then
        echo -e "${RED}❌ Error: adb not found. Please install Android SDK Platform Tools.${NC}"
        exit 1
    fi
    
    if ! command -v emulator &> /dev/null; then
        echo -e "${RED}❌ Error: emulator not found. Please install Android SDK Emulator.${NC}"
        exit 1
    fi
}

# Function to check if an emulator is running
check_emulator_running() {
    local running_devices=$(adb devices | grep -E "emulator.*device$" | wc -l)
    echo $running_devices
}

# Function to start an emulator
start_emulator() {
    echo -e "${YELLOW}📱 No Android emulator running. Starting one...${NC}"
    
    # List available AVDs
    local avds=$(emulator -list-avds)
    
    if [ -z "$avds" ]; then
        echo -e "${RED}❌ No Android Virtual Devices (AVDs) found.${NC}"
        echo -e "${YELLOW}💡 Please create an AVD using Android Studio or avdmanager.${NC}"
        exit 1
    fi
    
    # Get first available AVD
    local first_avd=$(echo "$avds" | head -n1)
    echo -e "${BLUE}🚀 Starting emulator with AVD: $first_avd${NC}"
    
    # Start emulator in background
    emulator -avd "$first_avd" -no-snapshot-save -no-audio &
    local emulator_pid=$!
    
    echo -e "${YELLOW}⏳ Waiting for emulator to boot up...${NC}"
    
    # Wait for emulator to be ready (timeout after 2 minutes)
    local timeout=120
    local elapsed=0
    while [ $elapsed -lt $timeout ]; do
        if [ $(check_emulator_running) -gt 0 ]; then
            # Wait a bit more for full boot
            sleep 10
            if adb shell getprop sys.boot_completed 2>/dev/null | grep -q "1"; then
                echo -e "${GREEN}✅ Emulator is ready!${NC}"
                return 0
            fi
        fi
        sleep 5
        elapsed=$((elapsed + 5))
        echo -e "${BLUE}⏳ Still waiting... ($elapsed/${timeout}s)${NC}"
    done
    
    echo -e "${RED}❌ Timeout waiting for emulator to start${NC}"
    exit 1
}

# Function to build and deploy the app
build_and_deploy() {
    echo -e "${BLUE}🔨 Building Golf Tracker Mobile app...${NC}"
    
    cd GolfTrackerApp.Mobile
    
    # Clear any existing logs
    adb logcat -c
    
    # Build and deploy to Android
    echo -e "${YELLOW}📦 Building and deploying to Android...${NC}"
    dotnet build -t:Run -f net9.0-android -p:AndroidSdkDirectory="$ANDROID_HOME"
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ Build or deployment failed!${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}✅ App deployed successfully!${NC}"
    cd ..
}

# Function to start logging
start_logging() {
    echo -e "${BLUE}📋 Starting Android logging...${NC}"
    echo -e "${YELLOW}💡 Look for authentication, HTTP, and error messages${NC}"
    echo -e "${YELLOW}💡 Press Ctrl+C to stop logging${NC}"
    echo "=================================="
    
    # Filter for relevant log messages
    adb logcat | grep -E "(GolfTracker|MAUI|Blazor|Authentication|HTTP|Bearer|JWT|OAuth|GoogleAuth|Dashboard|API|Exception|Error|auth|token)" --line-buffered
}

# Main execution
echo -e "${BLUE}🔍 Checking Android development environment...${NC}"
check_android_tools

echo -e "${BLUE}📱 Checking for running Android emulator...${NC}"
running_count=$(check_emulator_running)

if [ $running_count -eq 0 ]; then
    start_emulator
else
    echo -e "${GREEN}✅ Found $running_count running Android emulator(s)${NC}"
fi

# Wait a moment for device to be fully ready
sleep 3

build_and_deploy
start_logging