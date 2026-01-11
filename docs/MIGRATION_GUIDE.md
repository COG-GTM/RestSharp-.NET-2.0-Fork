# RestSharp .NET Core Migration Guide

## Overview

This document provides comprehensive guidance for migrating RestSharp from legacy .NET Framework to .NET Standard 2.0/.NET Core. It covers dependency changes, breaking changes, testing framework migration, and recommended approaches.

## Table of Contents

1. [Target Framework](#target-framework)
2. [Dependency Changes](#dependency-changes)
3. [JSON Serialization Migration](#json-serialization-migration)
4. [Testing Framework Migration](#testing-framework-migration)
5. [Conditional Compilation](#conditional-compilation)
6. [File Structure Changes](#file-structure-changes)
7. [Breaking Changes](#breaking-changes)
8. [Migration Scripts](#migration-scripts)
9. [CI/CD Pipeline](#cicd-pipeline)

## Target Framework

### Current State
The legacy RestSharp codebase targets multiple frameworks:
- .NET Framework 3.5 (main library)
- .NET Framework 2.0 (RestSharp.Net2)
- Silverlight 4.0
- Windows Phone 7.0/7.1 (Mango)
- MonoTouch (iOS)
- MonoDroid (Android)

### Target State
- **Primary Target:** .NET Standard 2.0
- **Test Projects:** .NET 8.0

### Why .NET Standard 2.0?
.NET Standard 2.0 provides the widest compatibility:
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5/6/7/8+
- Xamarin.iOS 10.14+
- Xamarin.Android 8.0+
- Unity 2018.1+

## Dependency Changes

### Newtonsoft.Json

| Platform | Legacy Version | New Version |
|----------|---------------|-------------|
| .NET 3.5 | 4.5.1 | 13.0.3 |
| Silverlight | 4.0.8 | 13.0.3 |
| Windows Phone | 4.0.8 | 13.0.3 |

**Key Changes in Newtonsoft.Json 13.0+:**
- Improved performance and memory usage
- Better handling of nullable reference types
- Enhanced security features
- Full .NET Standard 2.0 support

### System.Text.Json (Alternative)

System.Text.Json is now available as an alternative to Newtonsoft.Json:
- Version: 8.0.5
- Benefits: Native .NET integration, better performance for simple scenarios
- Considerations: Different API, some features require additional configuration

### Testing Framework

| Component | Legacy Version | New Version |
|-----------|---------------|-------------|
| xUnit | 1.9.0.1566 | 2.9.2 |
| xUnit.extensions | 1.9.0.1566 | (merged into xunit) |
| Test SDK | N/A | 17.11.1 |

## JSON Serialization Migration

### Newtonsoft.Json Migration

The migration from Newtonsoft.Json 4.x to 13.x is mostly backward compatible. Key considerations:

```csharp
// Legacy (4.x)
var json = JsonConvert.SerializeObject(obj);

// Modern (13.x) - Same API, enhanced features
var json = JsonConvert.SerializeObject(obj);

// With settings (recommended for consistency)
var settings = new JsonSerializerSettings
{
    ContractResolver = new CamelCasePropertyNamesContractResolver(),
    NullValueHandling = NullValueHandling.Ignore
};
var json = JsonConvert.SerializeObject(obj, settings);
```

### System.Text.Json Alternative

For new code, consider System.Text.Json:

```csharp
using System.Text.Json;

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
var json = JsonSerializer.Serialize(obj, options);
```

### Compatibility Considerations

| Feature | Newtonsoft.Json | System.Text.Json |
|---------|----------------|------------------|
| Circular references | Supported | Limited support |
| Custom converters | Rich API | Different API |
| Dynamic objects | Full support | Limited |
| Performance | Good | Better |
| Bundle size | Larger | Smaller |

**Recommendation:** Keep Newtonsoft.Json as primary serializer for backward compatibility. Offer System.Text.Json as an opt-in alternative.

## Testing Framework Migration

### xUnit 1.9 to 2.9 Migration

#### Attribute Changes

```csharp
// xUnit 1.9
[Fact]
public void TestMethod() { }

// xUnit 2.9 - Same syntax
[Fact]
public void TestMethod() { }
```

#### Assert Changes

```csharp
// xUnit 1.9
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => action());

// xUnit 2.9 - Same syntax, enhanced overloads
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Throws<Exception>(() => action());

// New in 2.x - Async support
await Assert.ThrowsAsync<Exception>(async () => await asyncAction());
```

#### Test Discovery

xUnit 2.x uses a different test discovery mechanism:
- Requires `Microsoft.NET.Test.Sdk` package
- Requires `xunit.runner.visualstudio` for Visual Studio integration

### Alternative Testing Frameworks

#### NUnit 3.x
```csharp
[Test]
public void TestMethod()
{
    Assert.That(actual, Is.EqualTo(expected));
}
```

#### MSTest v2
```csharp
[TestMethod]
public void TestMethod()
{
    Assert.AreEqual(expected, actual);
}
```

**Recommendation:** Use xUnit 2.9+ for consistency with existing tests. The migration is straightforward with minimal code changes.

## Conditional Compilation

### Legacy Directives

The legacy codebase uses these conditional compilation symbols:
- `FRAMEWORK` - Full .NET Framework
- `SILVERLIGHT` - Silverlight platform
- `WINDOWS_PHONE` - Windows Phone
- `MONOTOUCH` - iOS/MonoTouch
- `MONODROID` - Android/MonoDroid
- `PocketPC` - Windows Mobile

### Migration Strategy

For .NET Standard 2.0, use:
- `NETSTANDARD` - .NET Standard
- `NETSTANDARD2_0` - .NET Standard 2.0 specifically

Most `FRAMEWORK` code paths are compatible with .NET Standard 2.0. Platform-specific code (Silverlight, Windows Phone, etc.) should be removed.

### Example Migration

```csharp
// Legacy
#if FRAMEWORK
    // .NET Framework specific code
#elif SILVERLIGHT
    // Silverlight specific code
#elif WINDOWS_PHONE
    // Windows Phone specific code
#endif

// Migrated
#if NETSTANDARD2_0
    // .NET Standard 2.0 code (usually same as FRAMEWORK)
#endif
```

## File Structure Changes

### Legacy Structure (File Linking)

The legacy projects use file linking to share source code:

```xml
<!-- RestSharp.WindowsPhone.csproj -->
<Compile Include="..\RestSharp\RestClient.cs">
  <Link>RestClient.cs</Link>
</Compile>
```

### New Structure (Single Project)

The new .NET Standard 2.0 project consolidates all source files:

```
RestSharp.Core/
├── RestSharp.Core.csproj
├── Source/
│   ├── Authenticators/
│   ├── Deserializers/
│   ├── Extensions/
│   ├── Serializers/
│   └── *.cs
```

## Breaking Changes

### API Changes

1. **Removed Platform-Specific APIs**
   - Silverlight-specific async patterns
   - Windows Phone background transfer APIs
   - MonoTouch/MonoDroid specific implementations

2. **Async/Await Pattern**
   - Legacy: Callback-based async
   - Modern: Task-based async with async/await

3. **HTTP Client**
   - Legacy: HttpWebRequest
   - Modern: Consider HttpClient wrapper (optional)

### Behavioral Changes

1. **Default Serialization**
   - JSON date format may differ between Newtonsoft.Json versions
   - Recommend explicit date format configuration

2. **SSL/TLS**
   - .NET Standard 2.0 uses system default TLS version
   - Legacy may have used older TLS versions

## Migration Scripts

### Available Scripts

1. **consolidate-files.sh**
   - Copies source files from legacy project to new structure
   - Excludes AssemblyInfo and T4 templates
   - Usage: `./scripts/consolidate-files.sh [--dry-run] [--verbose]`

2. **remove-conditional-compilation.sh**
   - Marks platform-specific code for removal
   - Creates backups before modification
   - Usage: `./scripts/remove-conditional-compilation.sh [--dry-run] [--verbose]`

3. **update-dependencies.sh**
   - Checks for outdated NuGet packages
   - Reports recommended updates
   - Usage: `./scripts/update-dependencies.sh [--check-only] [--verbose]`

### Recommended Migration Order

1. Run `consolidate-files.sh --dry-run` to preview
2. Run `consolidate-files.sh` to copy files
3. Run `remove-conditional-compilation.sh --dry-run` to preview
4. Manually review and clean up conditional compilation
5. Build and fix compilation errors
6. Run tests and fix failures

## CI/CD Pipeline

### GitHub Actions Workflow

The new CI/CD pipeline (`.github/workflows/build.yml`) provides:

1. **Multi-Platform Build**
   - Ubuntu, Windows, macOS
   - Debug and Release configurations

2. **Automated Testing**
   - xUnit test execution
   - Code coverage collection

3. **NuGet Package Generation**
   - Automatic package creation on master branch
   - Symbol package generation

4. **Code Quality**
   - Build with analyzers
   - Format verification

### Running Locally

```bash
# Restore dependencies
dotnet restore RestSharp.Core.sln

# Build
dotnet build RestSharp.Core.sln --configuration Release

# Run tests
dotnet test RestSharp.Core.sln --configuration Release

# Create NuGet package
dotnet pack RestSharp.Core/RestSharp.Core.csproj --configuration Release
```

## Next Steps

After Phase 2 infrastructure is complete:

1. **Phase 3: Code Migration**
   - Run consolidation scripts
   - Remove conditional compilation
   - Fix compilation errors

2. **Phase 4: API Modernization**
   - Update async patterns
   - Add nullable reference type annotations
   - Consider HttpClient integration

3. **Phase 5: Testing & Validation**
   - Port all existing tests
   - Add new tests for .NET Standard scenarios
   - Performance benchmarking

4. **Phase 6: Release**
   - Update documentation
   - Create migration guide for users
   - Publish NuGet package
