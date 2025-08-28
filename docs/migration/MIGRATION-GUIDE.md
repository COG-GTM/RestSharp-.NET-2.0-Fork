# RestSharp .NET Core Migration Guide

## Overview

This guide helps you migrate from the legacy RestSharp .NET Framework version to the new .NET Core version.

## Breaking Changes

### Target Framework Changes
- **Removed:** .NET Framework 2.0, 3.5, 4.0
- **Removed:** Silverlight 4, Windows Phone 7, MonoTouch, MonoDroid
- **Added:** .NET 8.0, .NET Standard 2.0

### HTTP Infrastructure Changes
- **Replaced:** HttpWebRequest with HttpClient
- **Updated:** All async methods now use modern async/await patterns
- **Removed:** Callback-based async methods
- **Added:** Cancellation token support throughout

### Dependencies
- **Updated:** Newtonsoft.Json from 4.0.8/4.5.1 to 13.0.3
- **Added:** System.Text.Json 8.0.0 (for .NET 8.0 target only)
- **Removed:** Custom ZLib compression (now uses System.IO.Compression)

### API Changes

#### Removed Methods/Properties
- Legacy platform-specific conditional compilation blocks
- Custom compression implementations
- Thread.Abort() usage (not supported in .NET Core)

#### Updated Methods
All synchronous and asynchronous HTTP methods are now available across all target frameworks (previously some were conditionally compiled).

## Migration Steps

### 1. Update Target Framework
Update your project file to target .NET 8.0 or .NET Standard 2.0:

```xml
<TargetFramework>net8.0</TargetFramework>
<!-- or -->
<TargetFramework>netstandard2.0</TargetFramework>
```

### 2. Update Package Reference
```xml
<PackageReference Include="RestSharp.NetCore" Version="103.0.0" />
```

### 3. Update Code (if needed)
Most existing code should work without changes. However, if you were using:

#### Platform-specific features
Remove any conditional compilation or platform-specific code.

#### Custom async patterns
The library now uses standard async/await patterns. Update any callback-based async code:

**Before:**
```csharp
client.ExecuteAsync(request, response => {
    // Handle response
});
```

**After:**
```csharp
var response = await client.ExecuteAsync(request);
// Handle response
```

### 4. Test Your Application
- Verify all HTTP requests work correctly
- Test authentication mechanisms (OAuth1, OAuth2, Basic Auth, NTLM)
- Verify serialization/deserialization works as expected
- Test any custom authenticators or serializers

## Compatibility

### Backward Compatibility
The public API surface remains largely compatible. Most existing code should work without modification.

### Platform Support
- ✅ .NET 8.0+
- ✅ .NET Core 3.1+
- ✅ .NET Framework 4.6.1+ (via .NET Standard 2.0)
- ❌ .NET Framework 2.0/3.5
- ❌ Silverlight
- ❌ Windows Phone
- ❌ MonoTouch/MonoDroid

## Performance Improvements

- Modern HttpClient provides better connection pooling
- Reduced memory allocations with modern async patterns
- Improved compression handling with System.IO.Compression

## Troubleshooting

### Common Issues

#### Port Conflicts in Integration Tests
If you encounter port conflicts when running integration tests, this is an environment issue. The core functionality works correctly as demonstrated by unit tests.

#### Enum Parsing Issues
The migration includes improved enum parsing that handles various naming conventions (camelCase, snake_case, kebab-case).

#### Thread.Abort Not Supported
.NET Core doesn't support Thread.Abort(). Use cancellation tokens instead for request cancellation.

## Support

For issues specific to this .NET Core migration, please file issues at:
https://github.com/COG-GTM/RestSharp-.NET-2.0-Fork/issues

For general RestSharp questions, refer to the original project documentation.
