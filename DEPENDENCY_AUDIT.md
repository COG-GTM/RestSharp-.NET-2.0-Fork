# RestSharp .NET Core Migration - Dependency Audit

## Current Dependencies (Before Migration)

### Main Project (RestSharp.csproj)
- **Target Framework**: .NET Framework 3.5 Client Profile
- **Newtonsoft.Json**: 4.5.1 (from packages/Newtonsoft.Json.4.5.1/lib/net35/)
- **System References**:
  - System
  - System.Core (RequiredTargetFramework: 3.5)
  - System.ServiceModel.Web (RequiredTargetFramework: 3.5)
  - System.Xml.Linq (RequiredTargetFramework: 3.5)
  - System.Data
  - System.Xml

### Legacy Platform Projects (To Be Removed)

#### RestSharp.Net2
- **Target Framework**: .NET Framework 2.0
- **Newtonsoft.Json**: 4.5.1 (from packages/Newtonsoft.Json.4.5.1/lib/net20/)
- **Custom Implementation**: System.Xml.Linq (complete implementation for .NET 2.0)
- **Custom Implementation**: LinqBridge-1.2.cs for LINQ support

#### RestSharp.Silverlight
- **Target Framework**: Silverlight 4.0
- **Newtonsoft.Json**: 4.0.8 (from packages/Newtonsoft.Json.4.0.8/lib/sl4/)

#### RestSharp.WindowsPhone
- **Target Framework**: Windows Phone 7.0
- **Newtonsoft.Json**: 4.0.8 (from packages/Newtonsoft.Json.4.0.8/lib/sl3-wp/)

#### RestSharp.WindowsPhone.Mango
- **Target Framework**: Windows Phone 7.1
- **Newtonsoft.Json**: 4.0.8 (from packages/Newtonsoft.Json.4.0.8/lib/sl4-windowsphone71/)

#### RestSharp.MonoDroid
- **Target Framework**: MonoDroid (Android)
- **Custom Assembly**: Newtonsoft.Json.MonoDroid.dll (from References/)

#### RestSharp.MonoTouch
- **Target Framework**: MonoTouch (iOS)
- **Custom Assembly**: NewtonsoftJsonMonoTouch.dll (from References/)

## File Linking Architecture (To Be Eliminated)

All platform projects use extensive file linking with `<Link>` elements pointing back to the main RestSharp/ directory:
- **Total Linked Files**: ~200+ files per platform project
- **Linked Directories**: Authenticators/, Deserializers/, Extensions/, Serializers/, Validation/
- **Platform-Specific Files**: Only Properties/AssemblyInfo.cs and packages.config

## Migration Target Dependencies

### New Target Frameworks
- **.NET 6.0**: Latest LTS version with full feature support
- **.NET Standard 2.0**: Broad compatibility with .NET Framework and .NET Core

### Updated Dependencies
- **Newtonsoft.Json**: 13.0.3 (latest stable, compatible with both target frameworks)
- **System.ServiceModel.Primitives**: 4.10.3 (replacement for System.ServiceModel.Web in .NET Standard 2.0)

### Removed Dependencies
- All legacy platform-specific Newtonsoft.Json versions (4.0.8, custom assemblies)
- Custom System.Xml.Linq implementation (now built into target frameworks)
- LinqBridge (LINQ now native in target frameworks)

## Breaking Changes

### Newtonsoft.Json 4.5.1 → 13.0.3
- **API Changes**: Some obsolete methods removed, new serialization options added
- **Performance**: Significant performance improvements in newer version
- **Compatibility**: Maintains backward compatibility for core serialization scenarios

### System.ServiceModel.Web → System.ServiceModel.Primitives
- **Scope**: More focused package for .NET Standard 2.0 compatibility
- **Functionality**: Core WCF primitives maintained, some advanced features may require additional packages

## Migration Benefits

1. **Simplified Architecture**: Single project instead of 7 platform-specific projects
2. **Modern Tooling**: SDK-style project format with improved build performance
3. **Dependency Management**: PackageReference instead of packages.config
4. **Multi-Targeting**: Single codebase supporting multiple frameworks
5. **Maintenance**: Eliminated file linking reduces complexity and maintenance overhead
