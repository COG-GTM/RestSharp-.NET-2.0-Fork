# Conditional Compilation Map

## Phase 1 Migration Analysis - Preprocessor Directives

This document provides a comprehensive map of all conditional compilation directives (`#if`, `#else`, `#endif`) used throughout the RestSharp codebase, documenting what code is included or excluded for each directive and the functionality it provides.

---

## Table of Contents

1. [Directive Overview](#1-directive-overview)
2. [Directive Definitions by Project](#2-directive-definitions-by-project)
3. [Detailed Directive Usage](#3-detailed-directive-usage)
4. [File-by-File Analysis](#4-file-by-file-analysis)
5. [Migration Considerations](#5-migration-considerations)

---

## 1. Directive Overview

### Primary Directives

| Directive | Purpose | Platforms |
|-----------|---------|-----------|
| `FRAMEWORK` | Full .NET Framework features | .NET 3.5, .NET 2.0, MonoTouch, MonoDroid |
| `SILVERLIGHT` | Silverlight browser platform | Silverlight 4.0 |
| `WINDOWS_PHONE` | Windows Phone mobile platform | Windows Phone 7.1 |
| `MONOTOUCH` | MonoTouch iOS platform | MonoTouch |
| `MONODROID` | MonoDroid Android platform | MonoDroid |
| `NET_2_0` | .NET Framework 2.0 compatibility | .NET 2.0 |

### Directive Relationships

```
Platform Hierarchy:
├── FRAMEWORK (full features)
│   ├── .NET Framework 3.5
│   ├── .NET Framework 2.0 (+ NET_2_0)
│   ├── MonoTouch (+ MONOTOUCH)
│   └── MonoDroid (+ MONODROID)
├── SILVERLIGHT (limited features)
│   └── Silverlight 4.0
└── WINDOWS_PHONE (limited features + custom compression)
    └── Windows Phone 7.1 (+ MANGO)
```

---

## 2. Directive Definitions by Project

### RestSharp.csproj (.NET Framework 3.5)

```xml
<DefineConstants>TRACE;DEBUG;FRAMEWORK</DefineConstants>
```

### RestSharp.Net2.csproj (.NET Framework 2.0)

```xml
<DefineConstants>TRACE;DEBUG;Net2;FRAMEWORK;NET_2_0</DefineConstants>
```

### RestSharp.Silverlight.csproj (Silverlight 4.0)

```xml
<DefineConstants>DEBUG;TRACE;SILVERLIGHT</DefineConstants>
```

### RestSharp.WindowsPhone.Mango.csproj (Windows Phone 7.1)

```xml
<DefineConstants>TRACE;DEBUG;WINDOWS_PHONE;MANGO</DefineConstants>
```

### RestSharp.MonoTouch.csproj (iOS)

```xml
<DefineConstants>DEBUG;MONOTOUCH;FRAMEWORK</DefineConstants>
```

### RestSharp.MonoDroid.csproj (Android)

```xml
<DefineConstants>DEBUG;MONODROID;FRAMEWORK</DefineConstants>
```

---

## 3. Detailed Directive Usage

### 3.1 `#if FRAMEWORK`

**Purpose**: Enables full .NET Framework features not available on limited platforms.

**Features Enabled**:

| Feature | Location | Description |
|---------|----------|-------------|
| Synchronous HTTP Methods | `Http.Sync.cs` (entire file) | All sync HTTP operations |
| Synchronous Client Methods | `RestClient.Sync.cs` (entire file) | `Execute()`, `Execute<T>()`, `DownloadData()` |
| NTLM Authentication | `NtlmAuthenticator.cs` (entire file) | Windows authentication |
| Proxy Configuration | `IHttp.cs:56-63`, `Http.cs:154`, `Http.Async.cs:371-388` | `IWebProxy Proxy` property |
| Client Certificates | `IHttp.cs:34-37`, `Http.cs:119` | `X509CertificateCollection` |
| Max Redirects | `IHttp.cs:56-63`, `Http.cs:241` | `MaxRedirects` property |
| Follow Redirects | `Http.Async.cs:377-378` | `FollowRedirects` configuration |
| Automatic Decompression | `Http.Async.cs:371-378` | GZip/Deflate via `AutomaticDecompression` |
| Range Headers | `Http.cs:185-187` | Partial content requests |
| Request Credentials | `IRestRequest.cs:110` | `ICredentials Credentials` property |
| Timeout Configuration | `Http.Async.cs:371-374` | Request timeout setting |
| Assembly Version | `SharedAssemblyInfo.cs:10` | Assembly metadata |

**Code Examples**:

```csharp
// Http.Sync.cs - Entire file wrapped
#if FRAMEWORK
namespace RestSharp
{
    public partial class Http
    {
        public HttpResponse Post() { ... }
        public HttpResponse Get() { ... }
        // ... all sync methods
    }
}
#endif
```

```csharp
// IHttp.cs - Proxy and certificate properties
#if FRAMEWORK
    IWebProxy Proxy { get; set; }
    X509CertificateCollection ClientCertificates { get; set; }
    int MaxRedirects { get; set; }
#endif
```

```csharp
// Http.Async.cs - Request configuration
#if FRAMEWORK
    webRequest.Timeout = Timeout;
    if (Proxy != null) webRequest.Proxy = Proxy;
    webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
    webRequest.AllowAutoRedirect = FollowRedirects;
    if (FollowRedirects && MaxRedirects.HasValue)
        webRequest.MaximumAutomaticRedirections = MaxRedirects.Value;
#endif
```

### 3.2 `#if SILVERLIGHT`

**Purpose**: Handles Silverlight browser platform constraints.

**Features Affected**:

| Feature | Location | Description |
|---------|----------|-------------|
| WebRequest Creator | `Http.Async.cs:337-340` | Uses `WebRequestCreator.ClientHttp` |
| String Extensions | `StringExtensions.cs:29-31` | Platform-specific URL encoding |
| Collection Extensions | `OAuth/CollectionExtensions.cs:6` | LINQ compatibility |
| OAuth String Extensions | `OAuth/StringExtensions.cs:8, 119` | OAuth-specific encoding |

**Code Examples**:

```csharp
// Http.Async.cs - WebRequest registration
#if SILVERLIGHT
    WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
    WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
#endif
```

```csharp
// StringExtensions.cs - URL encoding
#if SILVERLIGHT
    return System.Windows.Browser.HttpUtility.UrlEncode(input);
#endif
```

### 3.3 `#if WINDOWS_PHONE`

**Purpose**: Handles Windows Phone mobile platform constraints and custom compression.

**Features Affected**:

| Feature | Location | Description |
|---------|----------|-------------|
| Custom GZip Decompression | `Http.cs:327-374` | Manual GZip stream handling |
| Content-Length Header | `Http.Async.cs:328-334` | POST request content length |
| String Extensions | `StringExtensions.cs:33-35` | Platform-specific URL encoding |
| ZLib Compression | `Compression/ZLib/*.cs` | Custom compression implementation |

**Code Examples**:

```csharp
// Http.cs - Custom GZip decompression
#if WINDOWS_PHONE
    if (response.Headers["Content-Encoding"] != null)
    {
        var encoding = response.Headers["Content-Encoding"].ToLower();
        if (encoding == "gzip")
        {
            var gzStream = new Ionic.Zlib.GZipStream(responseStream, 
                Ionic.Zlib.CompressionMode.Decompress);
            // ... decompress manually
        }
    }
#endif
```

```csharp
// Http.Async.cs - Content-Length for POST
#if WINDOWS_PHONE
    webRequest.ContentLength = bytes.Length;
#endif
```

### 3.4 `#if MONOTOUCH`

**Purpose**: MonoTouch (iOS) platform-specific code.

**Features Affected**:

| Feature | Location | Description |
|---------|----------|-------------|
| String Extensions | `StringExtensions.cs:36` | Combined with FRAMEWORK and MONODROID |

**Code Example**:

```csharp
// StringExtensions.cs - URL encoding
#if FRAMEWORK || MONOTOUCH || MONODROID
    return HttpUtility.UrlEncode(input);
#endif
```

### 3.5 `#if MONODROID`

**Purpose**: MonoDroid (Android) platform-specific code.

**Features Affected**:

| Feature | Location | Description |
|---------|----------|-------------|
| String Extensions | `StringExtensions.cs:36` | Combined with FRAMEWORK and MONOTOUCH |

### 3.6 `#if NET_2_0`

**Purpose**: .NET Framework 2.0 compatibility features.

**Features Affected**:

| Feature | Location | Description |
|---------|----------|-------------|
| Assembly Version | `SharedAssemblyInfo.cs:10` | Different version for .NET 2.0 |

**Code Example**:

```csharp
// SharedAssemblyInfo.cs
#if NET_2_0
[assembly: AssemblyVersion("102.4.0")]
#else
[assembly: AssemblyVersion("104.4.0")]
#endif
```

### 3.7 Negated Directives

#### `#if !SILVERLIGHT`

Used to exclude code from Silverlight:

```csharp
// IHttp.cs - FollowRedirects property
#if !SILVERLIGHT
    bool FollowRedirects { get; set; }
#endif
```

#### `#if SILVERLIGHT && !WindowsPhone`

Used for Silverlight-only code (not Windows Phone):

```csharp
// OAuth/StringExtensions.cs
#if SILVERLIGHT && !WindowsPhone
    // Silverlight-specific OAuth encoding
#endif
```

---

## 4. File-by-File Analysis

### Core HTTP Files

#### `RestSharp/Http.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 26-28 | `#if WINDOWS_PHONE` | Include Ionic.Zlib namespace |
| 119-121 | `#if FRAMEWORK` | ClientCertificates property |
| 154-156 | `#if FRAMEWORK` | Proxy property |
| 185-187 | `#if FRAMEWORK` | Range header handling |
| 227-229 | `#if FRAMEWORK` | FollowRedirects property |
| 241-243 | `#if FRAMEWORK` | MaxRedirects property |
| 321-325 | `#if FRAMEWORK` | Automatic decompression |
| 327-374 | `#if WINDOWS_PHONE` | Custom GZip decompression |
| 374-376 | `#if FRAMEWORK` | Response stream handling |

#### `RestSharp/Http.Async.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 22-25 | `#if SILVERLIGHT` | WebRequest registration |
| 27-28 | `#if WINDOWS_PHONE` | Include Ionic.Zlib namespace |
| 221-223 | `#if FRAMEWORK` | Timeout configuration |
| 325-327 | `#if SILVERLIGHT` | WebRequest creator registration |
| 328-334 | `#if WINDOWS_PHONE` | Content-Length header |
| 337-340 | `#if SILVERLIGHT` | WebRequest prefix registration |
| 371-388 | `#if FRAMEWORK` | Full request configuration |

#### `RestSharp/Http.Sync.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 1-237 | `#if FRAMEWORK` | Entire file (sync methods) |

### Client Files

#### `RestSharp/RestClient.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 44-46 | `#if WINDOWS_PHONE` | UserAgent default |
| 218-220 | `#if FRAMEWORK` | Proxy property |
| 377-379 | `#if FRAMEWORK` | ClientCertificates property |

#### `RestSharp/RestClient.Sync.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 1-end | `#if FRAMEWORK` | Entire file (sync client methods) |

### Interface Files

#### `RestSharp/IHttp.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 34-37 | `#if FRAMEWORK` | ClientCertificates property |
| 56-63 | `#if FRAMEWORK` | Proxy, MaxRedirects, sync methods |

#### `RestSharp/IRestRequest.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 110-112 | `#if FRAMEWORK` | Credentials property |

#### `RestSharp/IRestClient.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 68-70 | `#if FRAMEWORK` | Proxy property |

### Authentication Files

#### `RestSharp/Authenticators/NtlmAuthenticator.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 17-end | `#if FRAMEWORK` | Entire file (NTLM auth) |

#### `RestSharp/Authenticators/OAuth1Authenticator.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 7-9 | `#if WINDOWS_PHONE` | SHA1Managed import |

### Extension Files

#### `RestSharp/Extensions/StringExtensions.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 29-31 | `#if SILVERLIGHT` | Silverlight URL encoding |
| 33-35 | `#if WINDOWS_PHONE` | Windows Phone URL encoding |
| 36-38 | `#if FRAMEWORK \|\| MONOTOUCH \|\| MONODROID` | Framework URL encoding |
| 69-71 | `#if FRAMEWORK` | Additional string utilities |

#### `RestSharp/Extensions/MiscExtensions.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 115-117 | `#if FRAMEWORK` | Stream utilities |

#### `RestSharp/Extensions/ReflectionExtensions.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 68-70 | `#if FRAMEWORK` | Reflection utilities |
| 77-79 | `#if FRAMEWORK` | Type conversion |
| 94-96 | `#if FRAMEWORK` | Attribute retrieval |

### Other Files

#### `RestSharp/FileParameter.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 21-25 | `#if FRAMEWORK` | LongLength vs Length for byte arrays |

#### `RestSharp/SharedAssemblyInfo.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 10-14 | `#if NET_2_0` | Assembly version differentiation |

### Compression Files (Windows Phone Only)

All files in `RestSharp/Compression/ZLib/` are wrapped with `#if WINDOWS_PHONE`:

- `Crc32.cs:35`
- `FlushType.cs:1`
- `GZipStream.cs:29`
- `Inflate.cs:64`
- `InfTree.cs:62`
- `ZLib.cs:65`
- `ZLibCodec.cs:66`
- `ZLibConstants.cs:63`
- `ZLibStream.cs:28`

### OAuth Extension Files

#### `RestSharp/Authenticators/OAuth/Extensions/CollectionExtensions.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 6-8 | `#if SILVERLIGHT` | LINQ compatibility |

#### `RestSharp/Authenticators/OAuth/Extensions/StringExtensions.cs`

| Line(s) | Directive | Purpose |
|---------|-----------|---------|
| 8-10 | `#if SILVERLIGHT && !WindowsPhone` | Silverlight-only encoding |
| 119-121 | `#if SILVERLIGHT` | OAuth encoding |

---

## 5. Migration Considerations

### Directives to Remove in .NET Core

| Directive | Reason | Action |
|-----------|--------|--------|
| `SILVERLIGHT` | Platform deprecated | Remove all Silverlight-specific code |
| `WINDOWS_PHONE` | Platform deprecated | Remove all Windows Phone-specific code |
| `MONOTOUCH` | Replaced by Xamarin.iOS | Consider .NET MAUI support |
| `MONODROID` | Replaced by Xamarin.Android | Consider .NET MAUI support |
| `NET_2_0` | Legacy platform | Remove .NET 2.0 compatibility code |

### Directives to Potentially Keep/Modify

| Directive | Reason | Action |
|-----------|--------|--------|
| `FRAMEWORK` | Core functionality | Rename to `NETFRAMEWORK` or remove (make default) |

### Code to Migrate

1. **Synchronous Methods** (`Http.Sync.cs`, `RestClient.Sync.cs`)
   - Keep synchronous methods but implement using `HttpClient`
   - Consider Task-based async with `.GetAwaiter().GetResult()` for sync wrappers

2. **Proxy Configuration**
   - `HttpClient` supports proxy via `HttpClientHandler.Proxy`
   - API should remain similar

3. **Client Certificates**
   - `HttpClientHandler.ClientCertificates` provides equivalent functionality
   - API should remain similar

4. **Automatic Decompression**
   - `HttpClientHandler.AutomaticDecompression` provides equivalent functionality
   - Remove custom ZLib implementation

5. **NTLM Authentication**
   - `HttpClientHandler.Credentials` supports NTLM
   - May need `HttpClientHandler.UseDefaultCredentials`

### New Directives for .NET Core

Consider adding:

```csharp
#if NETCOREAPP
    // .NET Core specific code
#endif

#if NETSTANDARD
    // .NET Standard specific code
#endif

#if NET5_0_OR_GREATER
    // .NET 5+ specific code
#endif
```

### Recommended Migration Pattern

```csharp
// Before (current)
#if FRAMEWORK
    webRequest.Proxy = Proxy;
#endif

// After (.NET Core)
#if NETFRAMEWORK
    webRequest.Proxy = Proxy;
#else
    handler.Proxy = Proxy;
#endif
```

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
