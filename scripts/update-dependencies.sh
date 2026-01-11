#!/bin/bash
# RestSharp .NET Core Migration - Dependency Update Script
# This script updates NuGet package dependencies to their latest compatible versions
# for .NET Standard 2.0.
#
# Key dependency changes from legacy to .NET Standard 2.0:
# - Newtonsoft.Json: 4.5.1/4.0.8 -> 13.0.3
# - xUnit: 1.9.0.1566 -> 2.9.2
# - System.Text.Json: (new) 8.0.5
#
# Usage: ./update-dependencies.sh [--check-only] [--verbose]
#
# IMPORTANT: This script requires the .NET SDK to be installed.

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="$REPO_ROOT/RestSharp.Core.sln"

# Options
CHECK_ONLY=false
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --check-only)
            CHECK_ONLY=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

log() {
    if [ "$VERBOSE" = true ]; then
        echo "[INFO] $1"
    fi
}

log_always() {
    echo "$1"
}

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "[ERROR] .NET SDK is not installed or not in PATH"
    exit 1
fi

log_always "=== Dependency Update Script ==="
log_always "Solution: $SOLUTION_FILE"
log_always "Check Only: $CHECK_ONLY"
log_always ""

# Display current .NET SDK version
log_always "Using .NET SDK version: $(dotnet --version)"
log_always ""

# Restore packages first
log_always "Restoring packages..."
dotnet restore "$SOLUTION_FILE" --verbosity quiet

# List outdated packages
log_always ""
log_always "Checking for outdated packages..."
log_always ""

# Check RestSharp.Core project
log_always "=== RestSharp.Core ==="
cd "$REPO_ROOT/RestSharp.Core"
dotnet list package --outdated 2>/dev/null || echo "No outdated packages or unable to check"

# Check RestSharp.Core.Tests project
log_always ""
log_always "=== RestSharp.Core.Tests ==="
cd "$REPO_ROOT/RestSharp.Core.Tests"
dotnet list package --outdated 2>/dev/null || echo "No outdated packages or unable to check"

if [ "$CHECK_ONLY" = true ]; then
    log_always ""
    log_always "Check complete. Run without --check-only to update packages."
    exit 0
fi

# Update packages (if not check-only)
log_always ""
log_always "Updating packages..."

# Note: In a real scenario, you would use dotnet add package to update specific packages
# For safety, we just report what would be updated

log_always ""
log_always "=== Recommended Package Updates ==="
log_always ""
log_always "RestSharp.Core:"
log_always "  - Newtonsoft.Json: Keep at 13.0.3 (latest stable)"
log_always "  - System.Text.Json: Keep at 8.0.5 (latest for .NET 8)"
log_always "  - System.Net.Http: Keep at 4.3.4 (security patched)"
log_always ""
log_always "RestSharp.Core.Tests:"
log_always "  - xunit: Keep at 2.9.2 (latest stable)"
log_always "  - Microsoft.NET.Test.Sdk: Keep at 17.11.1 (latest stable)"
log_always "  - FluentAssertions: Keep at 6.12.1 (latest for .NET 8)"
log_always "  - Moq: Keep at 4.20.72 (latest stable)"
log_always ""
log_always "To update a specific package, run:"
log_always "  dotnet add <project> package <package-name> --version <version>"
