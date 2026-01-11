#!/bin/bash
# RestSharp .NET Core Migration - File Consolidation Script
# This script consolidates linked source files from the legacy RestSharp project
# into the new RestSharp.Core project structure.
#
# The legacy projects (WindowsPhone, Silverlight, etc.) use file linking to share
# source code. This script copies all source files to a single location for the
# .NET Standard 2.0 migration.
#
# Usage: ./consolidate-files.sh [--dry-run] [--verbose]
#
# IMPORTANT: This script is designed to be run in isolation and will NOT modify
# the original source files. It creates copies in the RestSharp.Core directory.

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
SOURCE_DIR="$REPO_ROOT/RestSharp"
TARGET_DIR="$REPO_ROOT/RestSharp.Core/Source"
BACKUP_DIR="$REPO_ROOT/RestSharp.Core/Source.backup"

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

# Validate source directory exists
if [ ! -d "$SOURCE_DIR" ]; then
    echo "[ERROR] Source directory not found: $SOURCE_DIR"
    exit 1
fi

log_always "=== RestSharp File Consolidation Script ==="
log_always "Source: $SOURCE_DIR"
log_always "Target: $TARGET_DIR"
log_always "Dry Run: $DRY_RUN"
log_always ""

# Create backup of existing target if it exists
if [ -d "$TARGET_DIR" ] && [ "$DRY_RUN" = false ]; then
    log_always "Creating backup of existing target directory..."
    rm -rf "$BACKUP_DIR"
    mv "$TARGET_DIR" "$BACKUP_DIR"
fi

# Create target directory structure
if [ "$DRY_RUN" = false ]; then
    mkdir -p "$TARGET_DIR"
fi

# Define directories to copy
DIRECTORIES=(
    "Authenticators"
    "Authenticators/OAuth"
    "Authenticators/OAuth/Extensions"
    "Compression"
    "Compression/ZLib"
    "Deserializers"
    "Extensions"
    "Extensions/MonoHttp"
    "Serializers"
    "Validation"
)

# Create directory structure
log_always "Creating directory structure..."
for dir in "${DIRECTORIES[@]}"; do
    if [ "$DRY_RUN" = false ]; then
        mkdir -p "$TARGET_DIR/$dir"
    fi
    log "  Created: $TARGET_DIR/$dir"
done

# Copy source files
log_always "Copying source files..."
FILE_COUNT=0

# Find all .cs files in source directory
while IFS= read -r -d '' file; do
    # Get relative path
    rel_path="${file#$SOURCE_DIR/}"
    target_file="$TARGET_DIR/$rel_path"
    
    # Skip AssemblyInfo and SharedAssemblyInfo
    if [[ "$rel_path" == *"AssemblyInfo.cs" ]]; then
        log "  Skipping: $rel_path (AssemblyInfo)"
        continue
    fi
    
    # Skip T4 templates
    if [[ "$rel_path" == *".tt" ]]; then
        log "  Skipping: $rel_path (T4 template)"
        continue
    fi
    
    if [ "$DRY_RUN" = false ]; then
        # Ensure target directory exists
        target_dir="$(dirname "$target_file")"
        mkdir -p "$target_dir"
        
        # Copy file
        cp "$file" "$target_file"
    fi
    
    log "  Copied: $rel_path"
    ((FILE_COUNT++))
done < <(find "$SOURCE_DIR" -name "*.cs" -type f -print0)

log_always ""
log_always "=== Consolidation Complete ==="
log_always "Files processed: $FILE_COUNT"

if [ "$DRY_RUN" = true ]; then
    log_always ""
    log_always "This was a dry run. No files were actually copied."
    log_always "Run without --dry-run to perform the actual consolidation."
fi
