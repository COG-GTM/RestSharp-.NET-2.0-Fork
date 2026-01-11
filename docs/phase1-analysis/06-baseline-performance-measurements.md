# Baseline Performance Measurements

## Phase 1 Migration Analysis - Performance Baseline

This document establishes baseline performance measurements for the RestSharp library before migration to .NET Core. These measurements will serve as comparison points to ensure no performance regression occurs during migration.

---

## Table of Contents

1. [Environment Information](#1-environment-information)
2. [Build Performance](#2-build-performance)
3. [Test Execution Baseline](#3-test-execution-baseline)
4. [Recommended Performance Benchmarks](#4-recommended-performance-benchmarks)
5. [Migration Performance Goals](#5-migration-performance-goals)

---

## 1. Environment Information

### Analysis Environment

| Component | Value |
|-----------|-------|
| Operating System | Linux (Ubuntu) |
| Runtime | Mono 6.8.0.105 |
| Build Tool | xbuild 14.0 |
| Date | January 2026 |

### Target Build Environments

For comprehensive baseline measurements, the following environments should be used:

| Environment | OS | Framework | Purpose |
|-------------|-----|-----------|---------|
| Windows + VS 2019 | Windows 10/11 | .NET Framework 3.5 | Primary baseline |
| Windows + VS 2019 | Windows 10/11 | .NET Framework 4.8 | Compatibility check |
| Linux + Mono | Ubuntu 20.04+ | Mono 6.x | Cross-platform check |

### Build Environment Limitations

The current analysis environment (Linux with Mono) has the following limitations:

1. **No .NET Framework 3.5 SDK**: The main RestSharp project targets .NET Framework 3.5, which is not fully supported on Linux/Mono
2. **Limited Test Execution**: xUnit tests require Windows/.NET Framework for full execution
3. **No Visual Studio**: Build tooling is limited to xbuild/msbuild

**Note**: Full baseline measurements should be captured on a Windows environment with Visual Studio and .NET Framework 3.5 SDK installed.

---

## 2. Build Performance

### Successful Build: RestSharp.Net2

The .NET 2.0 project builds successfully with Mono:

```
Build Configuration: Debug | AnyCPU
Build Tool: xbuild 14.0 (Mono 6.8.0.105)
Build Time: 0.30 seconds
Warnings: 0
Errors: 0
```

### Build Metrics to Capture (Windows Environment)

| Project | Expected Build Time | Notes |
|---------|-------------------|-------|
| RestSharp.csproj | < 5 seconds | Main .NET 3.5 project |
| RestSharp.Net2.csproj | < 5 seconds | .NET 2.0 with LinqBridge |
| RestSharp.Silverlight.csproj | < 5 seconds | Silverlight 4.0 |
| RestSharp.WindowsPhone.Mango.csproj | < 5 seconds | Windows Phone 7.1 |
| RestSharp.MonoTouch.csproj | < 5 seconds | iOS (requires Mac) |
| RestSharp.MonoDroid.csproj | < 5 seconds | Android |
| RestSharp.Tests.csproj | < 10 seconds | Unit tests |
| RestSharp.IntegrationTests.csproj | < 10 seconds | Integration tests |
| Full Solution | < 30 seconds | All projects |

---

## 3. Test Execution Baseline

### Test Projects Overview

| Project | Test Count | Framework |
|---------|------------|-----------|
| RestSharp.Tests | ~90 tests | xUnit 1.9.1 |
| RestSharp.IntegrationTests | ~10 tests | xUnit 1.9.1 |

### Expected Test Execution Times

Based on test complexity analysis:

| Test Category | Test Count | Expected Time |
|---------------|------------|---------------|
| JSON Deserialization | 29 | < 2 seconds |
| XML Deserialization | 44 | < 3 seconds |
| XML Serialization | 7 | < 1 second |
| URL Building | 10 | < 1 second |
| Authentication (Unit) | 2 | < 1 second |
| Async Operations | 2 | < 2 seconds |
| Compression | 3 | < 2 seconds |
| File Operations | 1 | < 1 second |
| **Total** | **~100** | **< 15 seconds** |

### Test Execution Commands

For Windows environment:

```powershell
# Build solution
msbuild RestSharp.sln /p:Configuration=Debug

# Run unit tests
packages\xunit.runners.1.9.1\tools\xunit.console.clr4.exe RestSharp.Tests\bin\Debug\RestSharp.Tests.dll

# Run integration tests
packages\xunit.runners.1.9.1\tools\xunit.console.clr4.exe RestSharp.IntegrationTests\bin\Debug\RestSharp.IntegrationTests.dll
```

---

## 4. Recommended Performance Benchmarks

### HTTP Operation Benchmarks

The following benchmarks should be established before migration:

#### 4.1 Request Creation Performance

| Operation | Target Time | Iterations |
|-----------|-------------|------------|
| Create RestClient | < 1 ms | 1000 |
| Create RestRequest | < 0.1 ms | 10000 |
| Add Parameter | < 0.01 ms | 100000 |
| Add Header | < 0.01 ms | 100000 |
| Build URI | < 0.1 ms | 10000 |

#### 4.2 Serialization Performance

| Operation | Data Size | Target Time |
|-----------|-----------|-------------|
| JSON Serialize (small) | 1 KB | < 1 ms |
| JSON Serialize (medium) | 100 KB | < 10 ms |
| JSON Serialize (large) | 1 MB | < 100 ms |
| XML Serialize (small) | 1 KB | < 1 ms |
| XML Serialize (medium) | 100 KB | < 20 ms |
| XML Serialize (large) | 1 MB | < 200 ms |

#### 4.3 Deserialization Performance

| Operation | Data Size | Target Time |
|-----------|-----------|-------------|
| JSON Deserialize (small) | 1 KB | < 1 ms |
| JSON Deserialize (medium) | 100 KB | < 15 ms |
| JSON Deserialize (large) | 1 MB | < 150 ms |
| XML Deserialize (small) | 1 KB | < 2 ms |
| XML Deserialize (medium) | 100 KB | < 30 ms |
| XML Deserialize (large) | 1 MB | < 300 ms |

#### 4.4 HTTP Request Performance

| Operation | Target Time | Notes |
|-----------|-------------|-------|
| Simple GET (localhost) | < 50 ms | Excludes network latency |
| Simple POST (localhost) | < 50 ms | Small payload |
| File Upload (1 MB) | < 200 ms | Excludes network |
| File Download (1 MB) | < 200 ms | Excludes network |

### Memory Usage Benchmarks

| Operation | Target Memory |
|-----------|---------------|
| RestClient instance | < 10 KB |
| RestRequest instance | < 5 KB |
| RestResponse (1 KB body) | < 20 KB |
| RestResponse (1 MB body) | < 2 MB |

---

## 5. Migration Performance Goals

### Performance Requirements

After migration to .NET Core, the following performance requirements must be met:

| Metric | Requirement |
|--------|-------------|
| Build Time | No more than 20% increase |
| Test Execution | No more than 10% increase |
| Request Creation | No regression |
| Serialization | No more than 10% regression |
| Deserialization | No more than 10% regression |
| HTTP Operations | Improvement expected (HttpClient) |
| Memory Usage | No more than 20% increase |

### Expected Improvements

Migration to .NET Core with HttpClient should provide:

| Area | Expected Improvement |
|------|---------------------|
| Async Performance | 20-50% improvement |
| Connection Pooling | Significant improvement |
| Memory Efficiency | 10-30% improvement |
| Startup Time | Faster cold start |
| Throughput | Higher concurrent requests |

### Benchmark Implementation

For migration validation, implement benchmarks using BenchmarkDotNet:

```csharp
[MemoryDiagnoser]
public class RestSharpBenchmarks
{
    private RestClient _client;
    private RestRequest _request;
    
    [GlobalSetup]
    public void Setup()
    {
        _client = new RestClient("http://localhost:8080");
        _request = new RestRequest("test");
    }
    
    [Benchmark]
    public Uri BuildUri() => _client.BuildUri(_request);
    
    [Benchmark]
    public RestRequest CreateRequest() => new RestRequest("test", Method.GET);
    
    [Benchmark]
    public void AddParameters()
    {
        var request = new RestRequest();
        for (int i = 0; i < 10; i++)
        {
            request.AddParameter($"param{i}", $"value{i}");
        }
    }
}
```

---

## Baseline Capture Checklist

Before starting migration, capture the following on a Windows environment:

- [ ] Full solution build time
- [ ] Individual project build times
- [ ] Unit test execution time
- [ ] Integration test execution time
- [ ] Test pass/fail counts
- [ ] Memory usage during tests
- [ ] Serialization benchmarks (JSON/XML)
- [ ] Deserialization benchmarks (JSON/XML)
- [ ] HTTP operation benchmarks (if possible)

---

## Summary

This document establishes the framework for baseline performance measurements. Due to environment limitations (Linux/Mono without full .NET Framework 3.5 support), complete baseline measurements should be captured on a Windows environment with:

1. Visual Studio 2019 or later
2. .NET Framework 3.5 SDK
3. .NET Framework 4.8 SDK
4. xUnit test runner

The measurements captured will serve as the comparison baseline for validating that the .NET Core migration does not introduce performance regressions.

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
