# RestSharp .NET Framework to .NET Core Migration - Dependency Audit

## Current Dependencies Analysis

### Main RestSharp Project (.NET Framework 3.5)
- **Newtonsoft.Json**: Version 4.5.1 (from packages\Newtonsoft.Json.4.5.1\lib\net35\)
- **System References**: System, System.Core, System.ServiceModel.Web, System.Xml.Linq, System.Data, System.Xml
- **Target Framework**: .NET Framework 3.5 Client Profile

### RestSharp.Net2 Project (.NET Framework 2.0)
- **Newtonsoft.Json**: Version 4.5.1 (from packages\Newtonsoft.Json.4.5.1\lib\net20\)
- **System References**: System, System.Data, System.Xml
- **Custom Components**: LinqBridge-1.2.cs, System.Xml.Linq implementation from Mono
- **Target Framework**: .NET Framework 2.0

### RestSharp.Silverlight Project
- **Newtonsoft.Json**: Version 4.0.8 (from packages\Newtonsoft.Json.4.0.8\lib\sl4\)
- **System References**: mscorlib, System.ServiceModel, System.ServiceModel.Web, System.Windows, System.Windows.Browser, System.Xml
- **Target Framework**: Silverlight 4.0

### RestSharp.WindowsPhone Projects
- **Newtonsoft.Json**: Version 4.0.8 (from packages\Newtonsoft.Json.4.0.8\lib\sl4-windowsphone71\)
- **System References**: Microsoft.Phone, System.Windows, System.Net, System.Xml
- **Target Framework**: Windows Phone 7.1

### RestSharp.MonoDroid Project
- **Newtonsoft.Json**: Custom MonoDroid assembly (Newtonsoft.Json.MonoDroid.dll)
- **System References**: mscorlib, System, System.Core, System.Xml, Mono.Android
- **Target Framework**: MonoAndroid

### RestSharp.MonoTouch Project
- **Newtonsoft.Json**: Custom MonoTouch assembly (Newtonsoft.Json.MonoTouch.dll)
- **System References**: mscorlib, System, System.Core, System.Xml, monotouch
- **Target Framework**: MonoTouch

## .NET Core Migration Plan

### Target Frameworks
- **Primary**: net6.0 (latest LTS)
- **Compatibility**: netstandard2.0 (broad compatibility)

### Dependency Updates
- **Newtonsoft.Json**: Upgrade to 13.0.3 (latest stable)
- **Alternative**: Consider System.Text.Json for .NET 6.0 target
- **HTTP**: Replace HttpWebRequest with HttpClient
- **Compression**: Replace custom ZLib with System.IO.Compression
- **Async**: Replace callback patterns with async/await

### Breaking Changes to Address
1. **HttpWebRequest → HttpClient**: Different API surface, lifecycle management
2. **Newtonsoft.Json 4.x → 13.x**: Potential serialization behavior changes
3. **Conditional Compilation**: Remove platform-specific code paths
4. **Custom ZLib**: Replace with built-in compression
5. **LinqBridge**: Remove .NET 2.0 compatibility layer

### Compatibility Strategy
- Maintain public API surface as much as possible
- Use multi-targeting to support both modern and legacy consumers
- Provide migration guide for breaking changes
- Ensure serialization compatibility with existing data

## Custom Components to Remove/Replace

### Custom ZLib Implementation
- Files: Crc32.cs, FlushType.cs, GZipStream.cs, Inflate.cs, InfTree.cs, ZLib.cs, ZLibCodec.cs, ZLibConstants.cs, ZLibStream.cs
- Replacement: System.IO.Compression.GZipStream, System.IO.Compression.DeflateStream

### LinqBridge (.NET 2.0 compatibility)
- File: LinqBridge-1.2.cs
- Replacement: Native LINQ support in .NET Standard 2.0+

### Mono System.Xml.Linq Implementation
- Files: System.Xml.Linq/*.cs (multiple files)
- Replacement: Native System.Xml.Linq in .NET Standard 2.0+

### MonoHttp Utilities
- Files: Extensions/MonoHttp/*.cs
- Evaluation needed: May still be useful for URL encoding

## Platform-Specific Code Removal

### Conditional Compilation Symbols to Remove
- FRAMEWORK
- SILVERLIGHT  
- WINDOWS_PHONE
- MONOTOUCH
- MONODROID
- NET_2_0

### Files with Platform-Specific Code
- Http.cs: Windows Phone GZip handling
- Http.Async.cs: Silverlight WebRequest registration, Windows Phone content length handling
- RestClient.cs: Windows Phone synchronization context
- All authenticators: Platform-specific imports and behavior

## Migration Verification Strategy

### Build Verification
- Ensure projects build successfully on both target frameworks
- Verify no legacy platform references remain
- Check that all conditional compilation is removed

### Functional Verification  
- HTTP operations (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS)
- Authentication mechanisms (OAuth1, OAuth2, Basic Auth, NTLM)
- Serialization/deserialization (JSON, XML)
- File uploads and multipart forms
- Compression/decompression
- Cookie handling
- Redirect following

### Performance Verification
- Compare HTTP throughput with HttpClient vs HttpWebRequest
- Memory usage patterns
- Async operation efficiency

### Compatibility Verification
- Existing consumer code compilation
- Serialization format compatibility
- API surface comparison
