# Platform Feature Matrix

## Phase 1 Migration Analysis - Platform Compatibility

This document provides a comprehensive matrix showing which features are available on each supported platform. This information is critical for understanding the migration scope and ensuring feature parity in .NET Core.

---

## Table of Contents

1. [Platform Overview](#1-platform-overview)
2. [Feature Availability Matrix](#2-feature-availability-matrix)
3. [Platform-Specific Implementation Details](#3-platform-specific-implementation-details)
4. [File Linking Architecture](#4-file-linking-architecture)
5. [Migration Implications](#5-migration-implications)

---

## 1. Platform Overview

### Supported Platforms

| Platform | Project File | Target Framework | Define Constants |
|----------|--------------|------------------|------------------|
| .NET Framework 3.5 | `RestSharp.csproj` | v3.5 | `FRAMEWORK` |
| .NET Framework 2.0 | `RestSharp.Net2.csproj` | v2.0 | `FRAMEWORK`, `NET_2_0` |
| Silverlight 4.0 | `RestSharp.Silverlight.csproj` | Silverlight v4.0 | `SILVERLIGHT` |
| Windows Phone 7.1 | `RestSharp.WindowsPhone.Mango.csproj` | Silverlight v4.0 (WP71 profile) | `WINDOWS_PHONE`, `MANGO` |
| MonoTouch (iOS) | `RestSharp.MonoTouch.csproj` | MonoTouch v1.0 | `MONOTOUCH`, `FRAMEWORK` |
| MonoDroid (Android) | `RestSharp.MonoDroid.csproj` | MonoDroid | `MONODROID`, `FRAMEWORK` |

### Platform Characteristics

| Platform | Sync Support | Async Support | Full HTTP Features | Notes |
|----------|--------------|---------------|-------------------|-------|
| .NET Framework 3.5 | Yes | Yes | Yes | Full feature set |
| .NET Framework 2.0 | Yes | Yes | Yes | Requires LinqBridge, custom System.Xml.Linq |
| Silverlight 4.0 | No | Yes | Limited | Browser security restrictions |
| Windows Phone 7.1 | No | Yes | Limited | Mobile constraints, custom compression |
| MonoTouch | Yes | Yes | Yes | iOS platform |
| MonoDroid | Yes | Yes | Yes | Android platform |

---

## 2. Feature Availability Matrix

### Core HTTP Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| Synchronous Requests | Yes | Yes | No | No | Yes | Yes |
| Asynchronous Requests | Yes | Yes | Yes | Yes | Yes | Yes |
| GET Method | Yes | Yes | Yes | Yes | Yes | Yes |
| POST Method | Yes | Yes | Yes | Yes | Yes | Yes |
| PUT Method | Yes | Yes | Yes | Yes | Yes | Yes |
| DELETE Method | Yes | Yes | Yes | Yes | Yes | Yes |
| HEAD Method | Yes | Yes | Yes | Yes | Yes | Yes |
| OPTIONS Method | Yes | Yes | Yes | Yes | Yes | Yes |
| PATCH Method | Yes | Yes | Yes | Yes | Yes | Yes |

### Connection Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| Follow Redirects | Yes | Yes | No | No | Yes | Yes |
| Max Redirects Config | Yes | Yes | No | No | Yes | Yes |
| Proxy Configuration | Yes | Yes | No | No | Yes | Yes |
| Client Certificates | Yes | Yes | No | No | Yes | Yes |
| Timeout Configuration | Yes | Yes | Yes | Yes | Yes | Yes |
| Cookie Container | Yes | Yes | Yes | Yes | Yes | Yes |
| Custom User Agent | Yes | Yes | Yes | Yes | Yes | Yes |

### Authentication Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| HTTP Basic Auth | Yes | Yes | Yes | Yes | Yes | Yes |
| OAuth 1.0a | Yes | Yes | No | Yes | Yes | Yes |
| OAuth 2.0 (Query) | Yes | Yes | Yes | Yes | Yes | Yes |
| OAuth 2.0 (Header) | Yes | Yes | Yes | Yes | Yes | Yes |
| NTLM Auth | Yes | Yes | No | No | No | No |
| Simple Auth | Yes | Yes | Yes | Yes | Yes | Yes |
| Custom Credentials | Yes | Yes | No | No | Yes | Yes |

### Serialization Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| JSON Serialization | Yes | Yes | Yes | Yes | Yes | Yes |
| JSON Deserialization | Yes | Yes | Yes | Yes | Yes | Yes |
| XML Serialization | Yes | Yes | Yes | Yes | Yes | Yes |
| XML Deserialization | Yes | Yes | Yes | Yes | Yes | Yes |
| Custom Serializers | Yes | Yes | Yes | Yes | Yes | Yes |
| DotNet XML Serializer | Yes | Yes | Yes | Yes | Yes | Yes |

### Compression Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| GZip Decompression | Yes (Auto) | Yes (Auto) | No | Yes (Custom) | Yes (Auto) | Yes (Auto) |
| Deflate Decompression | Yes (Auto) | Yes (Auto) | No | Yes (Custom) | Yes (Auto) | Yes (Auto) |
| Automatic Decompression | Yes | Yes | No | No | Yes | Yes |
| Custom ZLib Implementation | No | No | No | Yes | No | No |

### File Operations

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| File Upload | Yes | Yes | Yes | Yes | Yes | Yes |
| Multipart Form Data | Yes | Yes | Yes | Yes | Yes | Yes |
| Binary Download | Yes | Yes | Yes | Yes | Yes | Yes |
| File Parameter (LongLength) | Yes | No | No | No | Yes | Yes |

### Request Features

| Feature | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| URL Segment Parameters | Yes | Yes | Yes | Yes | Yes | Yes |
| Query String Parameters | Yes | Yes | Yes | Yes | Yes | Yes |
| Form Parameters | Yes | Yes | Yes | Yes | Yes | Yes |
| HTTP Headers | Yes | Yes | Yes | Yes | Yes | Yes |
| Request Body | Yes | Yes | Yes | Yes | Yes | Yes |
| Request Cookies | Yes | Yes | Yes | Yes | Yes | Yes |
| Range Headers | Yes | Yes | No | No | Yes | Yes |

---

## 3. Platform-Specific Implementation Details

### .NET Framework 3.5 (Full Feature Set)

The main project with complete functionality:

- Full synchronous and asynchronous HTTP support
- Complete proxy and certificate configuration
- NTLM authentication support
- Automatic GZip/Deflate decompression via `AutomaticDecompression`
- Range header support for partial content requests

**Key Files**:
- `Http.Sync.cs` - Synchronous HTTP methods
- `Http.Async.cs` - Asynchronous HTTP methods
- `RestClient.Sync.cs` - Synchronous client methods

### .NET Framework 2.0

Extended support for legacy .NET 2.0 environments:

- Includes LinqBridge for LINQ support
- Custom System.Xml.Linq implementation
- Same feature set as .NET 3.5

**Additional Files**:
- `LinqBridge-1.2.cs` - LINQ to Objects implementation
- `System.Xml.Linq/` - Complete XDocument/XElement implementation

### Silverlight 4.0

Browser-based platform with security restrictions:

- Async-only operations (no synchronous HTTP)
- No redirect following (browser handles)
- No proxy configuration
- No client certificates
- Uses `WebRequestCreator.ClientHttp` for cross-domain requests
- OAuth 1.0a not included (missing OAuth files)

**Excluded Features**:
- `Http.Sync.cs` not linked
- `RestClient.Sync.cs` not linked
- OAuth authenticator files not linked
- Compression files not linked

### Windows Phone 7.1 (Mango)

Mobile platform with custom compression:

- Async-only operations
- Custom ZLib implementation for GZip/Deflate
- Content-Length header handling for POST requests
- No redirect following
- No proxy configuration

**Custom Compression Files**:
- `Compression/ZLib/Crc32.cs`
- `Compression/ZLib/FlushType.cs`
- `Compression/ZLib/GZipStream.cs`
- `Compression/ZLib/Inflate.cs`
- `Compression/ZLib/InfTree.cs`
- `Compression/ZLib/ZLib.cs`
- `Compression/ZLib/ZLibCodec.cs`
- `Compression/ZLib/ZLibConstants.cs`
- `Compression/ZLib/ZLibStream.cs`

### MonoTouch (iOS)

iOS platform with full feature set:

- Full synchronous and asynchronous support
- Uses `FRAMEWORK` define constant
- Custom Newtonsoft.Json build (`NewtonsoftJsonMonoTouch.dll`)

### MonoDroid (Android)

Android platform with full feature set:

- Full synchronous and asynchronous support
- Uses `FRAMEWORK` define constant (assumed based on MonoTouch pattern)

---

## 4. File Linking Architecture

The RestSharp solution uses a hub-and-spoke architecture where platform-specific projects link to shared source files from the main `RestSharp/` directory.

### Shared Core Files (All Platforms)

These files are linked by all platform projects:

| Category | Files |
|----------|-------|
| Core | `Enum.cs`, `Parameter.cs`, `FileParameter.cs` |
| Client | `RestClient.cs`, `RestClient.Async.cs`, `RestRequest.cs`, `RestResponse.cs` |
| Interfaces | `IRestClient.cs`, `IRestRequest.cs`, `IRestResponse.cs`, `IHttp.cs`, `IHttpResponse.cs`, `IHttpFactory.cs` |
| HTTP | `Http.cs`, `Http.Async.cs`, `HttpCookie.cs`, `HttpFile.cs`, `HttpHeader.cs`, `HttpParameter.cs`, `HttpResponse.cs` |
| Serializers | `ISerializer.cs`, `JsonSerializer.cs`, `XmlSerializer.cs`, `SerializeAsAttribute.cs`, `DotNetXmlSerializer.cs` |
| Deserializers | `IDeserializer.cs`, `JsonDeserializer.cs`, `XmlDeserializer.cs`, `XmlAttributeDeserializer.cs`, `DeserializeAsAttribute.cs`, `DotNetXmlDeserializer.cs` |
| Auth | `IAuthenticator.cs`, `HttpBasicAuthenticator.cs`, `SimpleAuthenticator.cs`, `OAuth2Authenticator.cs` |
| Extensions | `MiscExtensions.cs`, `ReflectionExtensions.cs`, `StringExtensions.cs`, `XmlExtensions.cs` |
| Validation | `Require.cs`, `Validate.cs` |
| Other | `RestResponseCookie.cs`, `RestRequestAsyncHandle.cs`, `SharedAssemblyInfo.cs` |

### Platform-Specific File Inclusions

| File(s) | .NET 3.5 | .NET 2.0 | Silverlight | WP7.1 | MonoTouch | MonoDroid |
|---------|----------|----------|-------------|-------|-----------|-----------|
| `Http.Sync.cs` | Yes | Yes | No | No | Yes | Yes |
| `RestClient.Sync.cs` | Yes | Yes | No | No | Yes | Yes |
| `RestClientExtensions.cs` | Yes | Yes | Yes | Yes | No | No |
| `NtlmAuthenticator.cs` | Yes | Yes | Yes* | Yes* | Yes* | Yes* |
| `OAuth1Authenticator.cs` | Yes | Yes | No | Yes | Yes | Yes |
| OAuth Support Files | Yes | Yes | No | Yes | Yes | Yes |
| Compression/ZLib/* | No | Yes | No | Yes | No | No |
| MonoHttp/* | Yes | Yes | No | No | Yes | Yes |
| LinqBridge-1.2.cs | No | Yes | No | No | No | No |
| System.Xml.Linq/* | No | Yes | No | No | No | No |

*Note: `NtlmAuthenticator.cs` is linked but the code is wrapped in `#if FRAMEWORK`, so it compiles to empty on non-FRAMEWORK platforms.

### Link Syntax Example

From `RestSharp.Silverlight.csproj`:

```xml
<Compile Include="..\RestSharp\RestClient.cs">
  <Link>RestClient.cs</Link>
</Compile>
```

This creates a virtual link to the source file without copying it, ensuring all platforms compile from the same source.

---

## 5. Migration Implications

### Features to Preserve in .NET Core

All features currently available in .NET Framework 3.5 should be preserved:

1. **Full HTTP Method Support** - All methods (GET, POST, PUT, DELETE, HEAD, OPTIONS, PATCH)
2. **Synchronous and Asynchronous Operations** - Both patterns should be available
3. **Authentication** - All authenticators including NTLM
4. **Serialization** - JSON and XML with custom serializer support
5. **Compression** - Automatic GZip/Deflate handling
6. **File Operations** - Upload and download capabilities
7. **Connection Configuration** - Proxy, certificates, redirects, timeouts

### Features That May Change

1. **HttpWebRequest to HttpClient** - The underlying HTTP implementation will change
2. **Async Pattern** - Consider migrating to Task-based async (async/await)
3. **Platform Projects** - Silverlight, Windows Phone, and MonoTouch/MonoDroid projects will be deprecated
4. **Compression** - Custom ZLib implementation no longer needed (.NET Core has built-in support)

### Deprecated Platforms

The following platforms will not be supported in .NET Core:

| Platform | Reason | Migration Path |
|----------|--------|----------------|
| Silverlight 4.0 | End of life | N/A |
| Windows Phone 7.1 | End of life | N/A |
| MonoTouch | Replaced by Xamarin.iOS | Use .NET MAUI or Xamarin |
| MonoDroid | Replaced by Xamarin.Android | Use .NET MAUI or Xamarin |
| .NET Framework 2.0 | Legacy | Upgrade to .NET Core |

### New Platform Targets

.NET Core migration should target:

| Platform | Target Framework | Notes |
|----------|------------------|-------|
| .NET Core 3.1 | `netcoreapp3.1` | LTS version |
| .NET 5.0+ | `net5.0`, `net6.0`, etc. | Unified platform |
| .NET Standard 2.0 | `netstandard2.0` | Maximum compatibility |

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
