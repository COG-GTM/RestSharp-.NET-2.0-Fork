# Dependency Inventory

## Phase 1 Migration Analysis - External Dependencies

This document provides a comprehensive inventory of all external dependencies used by the RestSharp library across all platform targets, including version information and migration considerations.

---

## Table of Contents

1. [Dependency Overview](#1-dependency-overview)
2. [NuGet Package Dependencies](#2-nuget-package-dependencies)
3. [Framework Dependencies](#3-framework-dependencies)
4. [Platform-Specific Dependencies](#4-platform-specific-dependencies)
5. [Build and Test Dependencies](#5-build-and-test-dependencies)
6. [Migration Dependency Strategy](#6-migration-dependency-strategy)

---

## 1. Dependency Overview

### Primary External Dependencies

| Dependency | Purpose | Used By |
|------------|---------|---------|
| Newtonsoft.Json | JSON serialization/deserialization | All platforms |

### Dependency Summary by Project

| Project | External NuGet Packages | Framework References |
|---------|------------------------|---------------------|
| RestSharp | Newtonsoft.Json 4.5.1 | System, System.Xml, System.Xml.Linq |
| RestSharp.Net2 | Newtonsoft.Json 4.5.1 | System, System.Data, System.Xml |
| RestSharp.Silverlight | Newtonsoft.Json 4.0.8 | System, System.Xml, System.Net, System.Xml.Linq |
| RestSharp.WindowsPhone.Mango | Newtonsoft.Json 4.0.8 | System, System.Xml, System.Net, System.Xml.Linq |
| RestSharp.MonoTouch | NewtonsoftJsonMonoTouch | System, System.Xml, System.Core, System.Xml.Linq |
| RestSharp.MonoDroid | (Not analyzed) | System, System.Xml, System.Core |

---

## 2. NuGet Package Dependencies

### 2.1 Newtonsoft.Json

The primary external dependency for JSON processing.

#### Version Matrix

| Project | Version | Target Framework | NuGet Package Path |
|---------|---------|------------------|-------------------|
| RestSharp (.NET 3.5) | 4.5.1 | net35 | `packages\Newtonsoft.Json.4.5.1\lib\net35\Newtonsoft.Json.dll` |
| RestSharp.Net2 | 4.5.1 | net20 | `packages\Newtonsoft.Json.4.5.1\lib\net20\Newtonsoft.Json.dll` |
| RestSharp.Silverlight | 4.0.8 | sl4 | `packages\Newtonsoft.Json.4.0.8\lib\sl4\Newtonsoft.Json.dll` |
| RestSharp.WindowsPhone.Mango | 4.0.8 | sl4-windowsphone71 | `packages\Newtonsoft.Json.4.0.8\lib\sl4-windowsphone71\Newtonsoft.Json.dll` |
| RestSharp.MonoTouch | Custom | MonoTouch | `References\NewtonsoftJsonMonoTouch.dll` |

#### packages.config Files

**RestSharp/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="4.5.1" targetFramework="net35" />
</packages>
```

**RestSharp.Net2/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="4.5.1" targetFramework="net20" />
</packages>
```

**RestSharp.Silverlight/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="4.0.8" targetFramework="sl4" />
</packages>
```

**RestSharp.WindowsPhone.Mango/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Newtonsoft.Json" version="4.0.8" targetFramework="sl4-windowsphone71" />
</packages>
```

#### Newtonsoft.Json Usage in Code

| File | Usage |
|------|-------|
| `Serializers/JsonSerializer.cs` | `Newtonsoft.Json.JsonSerializer`, `JsonTextWriter`, `Formatting`, `MissingMemberHandling`, `NullValueHandling`, `DefaultValueHandling` |
| `Deserializers/JsonDeserializer.cs` | `Newtonsoft.Json.Linq.JToken`, `JObject`, `JArray`, `JValue` |

#### API Surface Used

```csharp
// Serialization
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// Deserialization
using Newtonsoft.Json.Linq;

// Classes/Enums Used:
- JsonSerializer
- JsonTextWriter
- Formatting (enum)
- MissingMemberHandling (enum)
- NullValueHandling (enum)
- DefaultValueHandling (enum)
- JToken
- JObject
- JArray
- JValue
```

---

## 3. Framework Dependencies

### 3.1 .NET Framework 3.5 (RestSharp.csproj)

```xml
<Reference Include="System" />
<Reference Include="System.Core" />
<Reference Include="System.Xml.Linq" />
<Reference Include="System.Data.DataSetExtensions" />
<Reference Include="System.Data" />
<Reference Include="System.Xml" />
```

| Assembly | Purpose |
|----------|---------|
| System | Core .NET types |
| System.Core | LINQ support |
| System.Xml.Linq | XDocument, XElement for XML processing |
| System.Data.DataSetExtensions | DataSet LINQ extensions |
| System.Data | Data access (minimal usage) |
| System.Xml | XmlReader, XmlWriter |

### 3.2 .NET Framework 2.0 (RestSharp.Net2.csproj)

```xml
<Reference Include="System" />
<Reference Include="System.Data" />
<Reference Include="System.Xml" />
```

| Assembly | Purpose |
|----------|---------|
| System | Core .NET types |
| System.Data | Data access |
| System.Xml | XML processing |

**Note**: .NET 2.0 project includes custom implementations:
- `LinqBridge-1.2.cs` - LINQ to Objects implementation
- `System.Xml.Linq/` - Complete XDocument/XElement implementation

### 3.3 Silverlight 4.0 (RestSharp.Silverlight.csproj)

```xml
<Reference Include="mscorlib" />
<Reference Include="System.ServiceModel" />
<Reference Include="System.ServiceModel.Web" />
<Reference Include="System.Windows" />
<Reference Include="system" />
<Reference Include="System.Core" />
<Reference Include="System.Xml" />
<Reference Include="System.Net" />
<Reference Include="System.Windows.Browser" />
<Reference Include="System.Xml.Linq" />
<Reference Include="System.Xml.Serialization" />
```

| Assembly | Purpose |
|----------|---------|
| mscorlib | Core runtime |
| System.ServiceModel | WCF support |
| System.ServiceModel.Web | Web services |
| System.Windows | Silverlight UI |
| System.Net | HTTP networking |
| System.Windows.Browser | Browser interop |
| System.Xml.Linq | XML processing |
| System.Xml.Serialization | XML serialization |

### 3.4 Windows Phone 7.1 (RestSharp.WindowsPhone.Mango.csproj)

```xml
<Reference Include="System.Windows" />
<Reference Include="system" />
<Reference Include="System.Core" />
<Reference Include="System.Xml" />
<Reference Include="System.Net" />
<Reference Include="mscorlib.extensions" />
<Reference Include="System.Xml.Linq" />
<Reference Include="System.Xml.Serialization" />
```

| Assembly | Purpose |
|----------|---------|
| System.Windows | Windows Phone UI |
| System.Net | HTTP networking |
| mscorlib.extensions | Runtime extensions |
| System.Xml.Linq | XML processing |
| System.Xml.Serialization | XML serialization |

### 3.5 MonoTouch (RestSharp.MonoTouch.csproj)

```xml
<Reference Include="System" />
<Reference Include="System.Xml" />
<Reference Include="System.Core" />
<Reference Include="monotouch" />
<Reference Include="System.Xml.Linq" />
```

| Assembly | Purpose |
|----------|---------|
| System | Core types |
| System.Xml | XML processing |
| System.Core | LINQ support |
| monotouch | iOS platform bindings |
| System.Xml.Linq | XDocument support |

---

## 4. Platform-Specific Dependencies

### 4.1 Custom ZLib Implementation (Windows Phone)

The Windows Phone project includes a custom ZLib compression library:

**Files**:
- `Compression/ZLib/Crc32.cs`
- `Compression/ZLib/FlushType.cs`
- `Compression/ZLib/GZipStream.cs`
- `Compression/ZLib/Inflate.cs`
- `Compression/ZLib/InfTree.cs`
- `Compression/ZLib/ZLib.cs`
- `Compression/ZLib/ZLibCodec.cs`
- `Compression/ZLib/ZLibConstants.cs`
- `Compression/ZLib/ZLibStream.cs`

**Namespace**: `Ionic.Zlib`

**Purpose**: Provides GZip/Deflate decompression on Windows Phone where `System.IO.Compression` is not available.

### 4.2 LinqBridge (.NET 2.0)

**File**: `RestSharp.Net2/LinqBridge-1.2.cs`

**Purpose**: Provides LINQ to Objects implementation for .NET 2.0 which doesn't have native LINQ support.

**Key Types Provided**:
- `Enumerable` extension methods
- `Func<>` delegates
- `Action<>` delegates
- LINQ query operators

### 4.3 System.Xml.Linq Implementation (.NET 2.0)

**Directory**: `RestSharp.Net2/System.Xml.Linq/`

**Files**:
- `Extensions.cs`
- `LoadOptions.cs`
- `ReaderOptions.cs`
- `SaveOptions.cs`
- `XAttribute.cs`
- `XCData.cs`
- `XComment.cs`
- `XContainer.cs`
- `XDeclaration.cs`
- `XDocument.cs`
- `XDocumentType.cs`
- `XElement.cs`
- `XIterators.cs`
- `XName.cs`
- `XNamespace.cs`
- `XNode.cs`
- `XNodeDocumentOrderComparer.cs`
- `XNodeEqualityComparer.cs`
- `XNodeNavigator.cs`
- `XNodeReader.cs`
- `XNodeWriter.cs`
- `XObject.cs`
- `XObjectChange.cs`
- `XObjectChangeEventArgs.cs`
- `XProcessingInstruction.cs`
- `XStreamingElement.cs`
- `XText.cs`
- `XUtil.cs`

**Purpose**: Complete implementation of System.Xml.Linq for .NET 2.0 which doesn't have this namespace.

### 4.4 MonoHttp Utilities

**Directory**: `RestSharp/Extensions/MonoHttp/`

**Files**:
- `Helpers.cs`
- `HtmlEncoder.cs`
- `HttpUtility.cs`

**Purpose**: HTTP utility functions from Mono project for URL encoding/decoding.

**Used By**: .NET Framework, .NET 2.0, MonoTouch, MonoDroid

---

## 5. Build and Test Dependencies

### 5.1 Build Tools

| Tool | Purpose | Location |
|------|---------|----------|
| ILMerge | Merge assemblies for distribution | `Tools/ILMerge/` |
| NuGet | Package management | `Tools/NuGet/` |

### 5.2 Test Dependencies

**RestSharp.Tests/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="xunit" version="1.9.1" targetFramework="net40" />
</packages>
```

**RestSharp.IntegrationTests/packages.config**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="xunit" version="1.9.1" targetFramework="net40" />
</packages>
```

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 1.9.1 | Unit testing framework |

### 5.3 Test Infrastructure

**SimpleServer** (`RestSharp.IntegrationTests/Helpers/`):
- Custom HTTP server for integration testing
- Uses `System.Net.HttpListener`
- No external dependencies

---

## 6. Migration Dependency Strategy

### 6.1 Newtonsoft.Json Migration

**Current State**: Newtonsoft.Json 4.5.1 (2012)

**Migration Options**:

| Option | Pros | Cons |
|--------|------|------|
| **Upgrade Newtonsoft.Json** | Minimal code changes, well-tested | Additional dependency |
| **Use System.Text.Json** | Built-in, no external dependency | API differences, migration effort |
| **Support Both** | Maximum flexibility | Increased complexity |

**Recommended Approach**: Upgrade to Newtonsoft.Json 13.x initially, then consider System.Text.Json as optional.

```xml
<!-- .NET Core migration -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### 6.2 Framework Reference Migration

| Current | .NET Core Equivalent |
|---------|---------------------|
| System | Microsoft.NETCore.App |
| System.Xml | System.Xml.ReaderWriter |
| System.Xml.Linq | System.Xml.XDocument |
| System.Net | System.Net.Http |
| System.Core | (Built-in) |

### 6.3 Dependencies to Remove

| Dependency | Reason |
|------------|--------|
| LinqBridge | LINQ built into .NET Core |
| Custom System.Xml.Linq | Built into .NET Core |
| Custom ZLib | System.IO.Compression available |
| Silverlight assemblies | Platform deprecated |
| Windows Phone assemblies | Platform deprecated |
| MonoTouch assemblies | Use Xamarin.iOS or .NET MAUI |

### 6.4 New Dependencies for .NET Core

```xml
<ItemGroup>
  <!-- Core HTTP -->
  <PackageReference Include="System.Net.Http" Version="4.3.4" />
  
  <!-- JSON (choose one) -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <!-- OR -->
  <PackageReference Include="System.Text.Json" Version="7.0.0" />
  
  <!-- XML (built-in, no package needed) -->
</ItemGroup>
```

### 6.5 Test Dependency Updates

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.4.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.4.0" />
  <PackageReference Include="coverlet.collector" Version="3.2.0" />
</ItemGroup>
```

### 6.6 Dependency Version Compatibility Matrix

| Dependency | .NET Framework 4.8 | .NET Core 3.1 | .NET 6.0 | .NET 7.0+ |
|------------|-------------------|---------------|----------|-----------|
| Newtonsoft.Json 13.x | Yes | Yes | Yes | Yes |
| System.Text.Json | 4.7.2+ | Yes | Yes | Yes |
| xunit 2.4.x | Yes | Yes | Yes | Yes |
| System.Net.Http | Built-in | Built-in | Built-in | Built-in |

---

## Dependency Risk Assessment

### High Risk Dependencies

| Dependency | Risk | Mitigation |
|------------|------|------------|
| Newtonsoft.Json (old version) | Security vulnerabilities | Upgrade to latest |
| Custom ZLib | Maintenance burden | Use built-in compression |
| LinqBridge | Unnecessary complexity | Remove for .NET Core |

### Medium Risk Dependencies

| Dependency | Risk | Mitigation |
|------------|------|------------|
| xunit (old version) | Missing features | Upgrade to 2.4.x |
| ILMerge | May not work with .NET Core | Use single-file publish |

### Low Risk Dependencies

| Dependency | Risk | Mitigation |
|------------|------|------------|
| System.Xml.Linq | Stable API | Direct migration |
| System.Net | API changes | Use HttpClient |

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
