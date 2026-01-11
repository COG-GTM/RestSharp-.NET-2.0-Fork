#!/bin/bash
# RestSharp .NET Core Migration - Conditional Compilation Removal Script
# This script removes platform-specific conditional compilation directives
# from the consolidated source files.
#
# The legacy RestSharp codebase uses conditional compilation for:
# - FRAMEWORK (full .NET Framework)
# - SILVERLIGHT (Silverlight platform)
# - WINDOWS_PHONE (Windows Phone platform)
# - MONOTOUCH (iOS/MonoTouch)
# - MONODROID (Android/MonoDroid)
# - PocketPC (Windows Mobile)
#
# For .NET Standard 2.0, we keep the FRAMEWORK code paths and remove
# platform-specific code that is not compatible.
#
# Usage: ./remove-conditional-compilation.sh [--dry-run] [--verbose] [--target-dir <path>]
#
# IMPORTANT: This script modifies files in place. Always run with --dry-run first
# to preview changes. The script creates backups before making modifications.

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
TARGET_DIR="$REPO_ROOT/RestSharp.Core/Source"
BACKUP_SUFFIX=".bak"

# Options
DRY_RUN=false
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --target-dir)
            TARGET_DIR="$2"
            shift 2
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

# Validate target directory exists
if [ ! -d "$TARGET_DIR" ]; then
    echo "[ERROR] Target directory not found: $TARGET_DIR"
    echo "Run consolidate-files.sh first to create the source directory."
    exit 1
fi

log_always "=== Conditional Compilation Removal Script ==="
log_always "Target: $TARGET_DIR"
log_always "Dry Run: $DRY_RUN"
log_always ""

# Define platform-specific directives to remove
# These are the #if/#elif/#else/#endif blocks for non-.NET Standard platforms
PLATFORM_DIRECTIVES=(
    "SILVERLIGHT"
    "WINDOWS_PHONE"
    "MONOTOUCH"
    "MONODROID"
    "PocketPC"
)

# Count files to process
FILE_COUNT=0
MODIFIED_COUNT=0

log_always "Scanning for conditional compilation directives..."

# Process each .cs file
while IFS= read -r -d '' file; do
    ((FILE_COUNT++))
    rel_path="${file#$TARGET_DIR/}"
    
    # Check if file contains any platform-specific directives
    has_directives=false
    for directive in "${PLATFORM_DIRECTIVES[@]}"; do
        if grep -q "#if.*$directive\|#elif.*$directive" "$file" 2>/dev/null; then
            has_directives=true
            break
        fi
    done
    
    if [ "$has_directives" = true ]; then
        log "  Processing: $rel_path"
        ((MODIFIED_COUNT++))
        
        if [ "$DRY_RUN" = false ]; then
            # Create backup
            cp "$file" "${file}${BACKUP_SUFFIX}"
            
            # Process the file to remove platform-specific code blocks
            # This is a simplified approach - for complex nested directives,
            # a more sophisticated parser would be needed
            
            # For now, we'll add comments to mark sections that need manual review
            for directive in "${PLATFORM_DIRECTIVES[@]}"; do
                sed -i "s/#if $directive/#if $directive \/\/ TODO: Remove for .NET Standard 2.0/g" "$file"
                sed -i "s/#elif $directive/#elif $directive \/\/ TODO: Remove for .NET Standard 2.0/g" "$file"
            done
        fi
        
        # Report directives found
        for directive in "${PLATFORM_DIRECTIVES[@]}"; do
            count=$(grep -c "#if.*$directive\|#elif.*$directive" "$file" 2>/dev/null || echo "0")
            if [ "$count" -gt 0 ]; then
                log "    Found $count occurrences of $directive"
            fi
        done
    fi
done < <(find "$TARGET_DIR" -name "*.cs" -type f -print0)

log_always ""
log_always "=== Processing Complete ==="
log_always "Files scanned: $FILE_COUNT"
log_always "Files with platform directives: $MODIFIED_COUNT"

if [ "$DRY_RUN" = true ]; then
    log_always ""
    log_always "This was a dry run. No files were modified."
    log_always "Run without --dry-run to perform the actual modifications."
fi

log_always ""
log_always "Note: This script marks platform-specific code for manual review."
log_always "Complex nested conditional compilation blocks may require manual cleanup."
