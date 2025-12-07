#!/bin/bash
# Generate directory-specific Directory.Build.props files with dynamic target framework values
# based on .NET SDK version. If any files differ from what's in git, commit and push, then exit with error.

set -e

# Create a temporary project to query SDK properties
TEMP_DIR=$(mktemp -d)
TEMP_PROJ="$TEMP_DIR/temp.csproj"

cat > "$TEMP_PROJ" << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

# Get NETCoreAppMaximumVersion from SDK
MAX_VERSION=$(dotnet msbuild "$TEMP_PROJ" -getProperty:NETCoreAppMaximumVersion 2>/dev/null | tail -1 | tr -d ' ')

# Clean up temp project
rm -rf "$TEMP_DIR"

# Extract major version (e.g., "10.0" -> "10")
MAX_MAJOR=$(echo "$MAX_VERSION" | cut -d. -f1)

# Determine minimum supported major version based on EOL dates
# EOL dates: 8=Nov2026, 9=May2026, 10=Nov2028, 11=May2028, 12=Nov2030, 13=May2030
# Policy: support all non-EOL versions when this SDK version is current
if [ "$MAX_MAJOR" -eq 9 ] || [ "$MAX_MAJOR" -eq 10 ]; then
    # .NET 9/10 release: 8, 9 still supported (9 EOL May 2026)
    MIN_MAJOR=8
elif [ "$MAX_MAJOR" -eq 11 ] || [ "$MAX_MAJOR" -eq 12 ]; then
    # .NET 11/12 release: 8/9 EOL, 10+ supported
    MIN_MAJOR=10
elif [ "$MAX_MAJOR" -eq 13 ] || [ "$MAX_MAJOR" -eq 14 ]; then
    # .NET 13/14 release: 10/11 still supported (11 EOL May 2028)
    MIN_MAJOR=10
else
    # Fallback for unknown versions
    MIN_MAJOR=$MAX_MAJOR
fi

# Build list of supported frameworks for library (multi-target)
LIB_FRAMEWORKS=""
for v in $(seq $MIN_MAJOR $MAX_MAJOR); do
    if [ -n "$LIB_FRAMEWORKS" ]; then
        LIB_FRAMEWORKS="${LIB_FRAMEWORKS};net${v}.0"
    else
        LIB_FRAMEWORKS="net${v}.0"
    fi
done

CHANGES_MADE=false
PROPS_FILES=""

# Generate DicomTypeTranslation/Directory.Build.props for library (multi-targeting)
LIB_PROPS="DicomTypeTranslation/Directory.Build.props"
TEMP_LIB=$(mktemp)
cat > "$TEMP_LIB" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Library projects multi-target all non-EOL .NET versions -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFrameworks>$LIB_FRAMEWORKS</TargetFrameworks>
  </PropertyGroup>
</Project>
EOF

if ! diff -q "$LIB_PROPS" "$TEMP_LIB" > /dev/null 2>&1; then
    echo "$LIB_PROPS needs updating for current .NET SDK version"
    mv "$TEMP_LIB" "$LIB_PROPS"
    CHANGES_MADE=true
    PROPS_FILES="$PROPS_FILES $LIB_PROPS"
else
    rm -f "$TEMP_LIB"
fi

# Generate DicomTypeTranslation.Tests/Directory.Build.props for tests (single-target latest)
TEST_PROPS="DicomTypeTranslation.Tests/Directory.Build.props"
TEMP_TEST=$(mktemp)
cat > "$TEMP_TEST" << EOF
<Project>
  <!-- Import parent props -->
  <Import Project="\$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '\$(MSBuildThisFileDirectory)../'))" />

  <!-- Test projects target only the latest .NET version -->
  <!-- Auto-generated based on SDK version by scripts/generate-build-props.sh -->
  <PropertyGroup>
    <TargetFramework>net${MAX_MAJOR}.0</TargetFramework>
  </PropertyGroup>
</Project>
EOF

if ! diff -q "$TEST_PROPS" "$TEMP_TEST" > /dev/null 2>&1; then
    echo "$TEST_PROPS needs updating for current .NET SDK version"
    mv "$TEMP_TEST" "$TEST_PROPS"
    CHANGES_MADE=true
    PROPS_FILES="$PROPS_FILES $TEST_PROPS"
else
    rm -f "$TEMP_TEST"
fi

# If changes were made and we're in CI, commit and push
if [ "$CHANGES_MADE" = true ]; then
    if [ -d .git ] && [ -n "$CI" ]; then
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        git add $PROPS_FILES
        git commit -m "Update Directory.Build.props files for .NET SDK version"
        git push
        echo "ERROR: Directory.Build.props files were out of date and have been updated."
        echo "The changes have been committed and pushed. Please retry the workflow."
        exit 1
    else
        echo "Updated props files locally. Please commit the changes."
        exit 0
    fi
else
    echo "All Directory.Build.props files are up to date"
    exit 0
fi
