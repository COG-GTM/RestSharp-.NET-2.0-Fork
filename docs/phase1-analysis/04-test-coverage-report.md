# Test Coverage Report

## Phase 1 Migration Analysis - Test Baseline

This document provides a comprehensive analysis of the existing test suite, documenting current test coverage, identifying gaps, and establishing a baseline for the migration to .NET Core.

---

## Table of Contents

1. [Test Project Overview](#1-test-project-overview)
2. [Unit Test Coverage](#2-unit-test-coverage)
3. [Integration Test Coverage](#3-integration-test-coverage)
4. [Coverage Gap Analysis](#4-coverage-gap-analysis)
5. [Recommended Test Additions](#5-recommended-test-additions)
6. [Migration Testing Strategy](#6-migration-testing-strategy)

---

## 1. Test Project Overview

### Test Projects in Solution

| Project | Type | Framework | Test Framework |
|---------|------|-----------|----------------|
| `RestSharp.Tests` | Unit Tests | .NET Framework | xUnit |
| `RestSharp.IntegrationTests` | Integration Tests | .NET Framework | xUnit |

### Test Project Structure

```
RestSharp.Tests/
├── JsonTests.cs           # JSON deserialization tests
├── XmlTests.cs            # XML deserialization tests
├── UrlBuilderTests.cs     # URL building tests
├── SerializerTests.cs     # XML serialization tests
├── NamespacedXmlTests.cs  # XML namespace handling tests
├── SampleClasses/         # Test data models
├── SampleData/            # Test JSON/XML files
├── Fakes/                 # Mock objects
└── Properties/

RestSharp.IntegrationTests/
├── AuthenticationTests.cs # Authentication tests
├── AsyncTests.cs          # Async operation tests
├── CompressionTests.cs    # Compression handling tests
├── FileTests.cs           # File download tests
├── StatusCodeTests.cs     # HTTP status code tests
├── oAuth1Tests.cs         # OAuth 1.0 tests
├── Helpers/               # Test utilities (SimpleServer)
└── Assets/                # Test files
```

### Test Dependencies

- **xUnit** - Test framework
- **SimpleServer** - Custom HTTP server for integration tests

---

## 2. Unit Test Coverage

### 2.1 JSON Deserialization Tests (`JsonTests.cs`)

**Test Count**: 29 tests

| Test Name | Coverage Area | Status |
|-----------|---------------|--------|
| `Can_Deserialize_4sq_Json_With_Root_Element_Specified` | Root element navigation | Active |
| `Can_Deserialize_Lists_of_Simple_Types` | List deserialization | Active |
| `Can_Deserialize_Simple_Generic_List_of_Simple_Types` | Generic list handling | Active |
| `Can_Deserialize_From_Root_Element` | Root element extraction | Active |
| `Can_Deserialize_Generic_Members` | Generic type handling | Active |
| `Can_Deserialize_Empty_Elements_to_Nullable_Values` | Nullable handling | Active |
| `Can_Deserialize_Elements_to_Nullable_Values` | Nullable with values | Active |
| `Can_Deserialize_Custom_Formatted_Date` | Custom date formats | Active |
| `Can_Deserialize_Root_Json_Array_To_List` | Array to list | Active |
| `Can_Deserialize_Various_Enum_Values` | Enum deserialization | Active |
| `Can_Deserialize_Guid_String_Fields` | GUID handling | Active |
| `Can_Deserialize_Quoted_Primitive` | Quoted numbers | Active |
| `Can_Deserialize_With_Default_Root` | Default root element | Active |
| `Can_Deserialize_Names_With_Underscores_With_Default_Root` | Underscore naming | Active |
| `Can_Deserialize_Names_With_Dashes_With_Default_Root` | Dash naming | Active |
| `Ignore_Protected_Property_That_Exists_In_Data` | Protected properties | Active |
| `Ignore_ReadOnly_Property_That_Exists_In_Data` | ReadOnly properties | Active |
| `Can_Deserialize_Iso_Json_Dates` | ISO 8601 dates | Active |
| `Can_Deserialize_JScript_Json_Dates` | JavaScript dates | Active |
| `Can_Deserialize_Unix_Json_Dates` | Unix timestamps | Active |
| `Can_Deserialize_JsonNet_Dates` | Json.NET date format | Active |
| `Can_Deserialize_DateTime` | DateTime handling | Active |
| `Can_Deserialize_Nullable_DateTime_With_Value` | Nullable DateTime | Active |
| `Can_Deserialize_Nullable_DateTime_With_Null` | Null DateTime | Active |
| `Can_Deserialize_DateTimeOffset` | DateTimeOffset | Active |
| `Can_Deserialize_Nullable_DateTimeOffset_With_Value` | Nullable DateTimeOffset | Active |
| `Can_Deserialize_Nullable_DateTimeOffset_With_Null` | Null DateTimeOffset | Active |
| `Can_Deserialize_To_Dictionary_String_String` | Dictionary handling | Active |

**Coverage Summary**:
- Root element handling: Covered
- List/Array deserialization: Covered
- Nullable types: Covered
- Date/Time formats: Covered (ISO, Unix, JavaScript, custom)
- Enum handling: Covered
- GUID handling: Covered
- Dictionary handling: Covered
- Fuzzy name matching: Covered (underscores, dashes)

### 2.2 XML Deserialization Tests (`XmlTests.cs`)

**Test Count**: 44+ tests

| Test Category | Tests | Coverage |
|---------------|-------|----------|
| List Deserialization | 10 | Inline lists, nested lists, empty lists |
| Nullable Values | 4 | Empty elements, populated elements |
| Custom Date Formats | 1 | Custom date format strings |
| Element Deserialization | 2 | Default root, attributes |
| Property Handling | 2 | Protected, ReadOnly properties |
| Name Matching | 4 | Underscores, dashes, case variations |
| Real-World XML | 4 | Eventful, Lastfm, Google Weather, Twilio |
| Boolean Handling | 2 | From number, from string |
| Attribute Handling | 2 | Empty with attributes, mixed |

**Coverage Summary**:
- List deserialization: Comprehensive
- Nullable handling: Covered
- Name matching: Covered (fuzzy matching)
- Real-world XML formats: Covered
- Attribute vs Element: Covered

### 2.3 XML Serialization Tests (`SerializerTests.cs`)

**Test Count**: 7 tests

| Test Name | Coverage Area |
|-----------|---------------|
| `Serializes_Properties_In_Specified_Order` | Property ordering |
| `Can_serialize_simple_POCO` | Basic serialization |
| `Can_serialize_simple_POCO_With_DateFormat_Specified` | Date formatting |
| `Can_serialize_simple_POCO_With_XmlFormat_Specified` | XML format options |
| `Can_serialize_simple_POCO_With_Different_Root_Element` | Custom root element |
| `Can_serialize_simple_POCO_With_Attribute_Options_Defined` | SerializeAsAttribute |
| `Can_serialize_a_list_which_is_the_root_element` | List serialization |

**Coverage Summary**:
- Basic POCO serialization: Covered
- Date formatting: Covered
- Custom naming: Covered
- Attribute serialization: Covered
- List serialization: Covered

### 2.4 URL Builder Tests (`UrlBuilderTests.cs`)

**Test Count**: 10 tests

| Test Name | Coverage Area |
|-----------|---------------|
| `GET_with_leading_slash` | Leading slash handling |
| `POST_with_leading_slash` | POST with leading slash |
| `GET_with_leading_slash_and_baseurl_trailing_slash` | Slash normalization |
| `POST_with_leading_slash_and_baseurl_trailing_slash` | POST slash handling |
| `GET_with_resource_containing_slashes` | Resource path slashes |
| `POST_with_resource_containing_slashes` | POST resource slashes |
| `GET_with_resource_containing_tokens` | URL segment replacement |
| `POST_with_resource_containing_tokens` | POST URL segments |
| `GET_with_empty_request` | Empty request handling |
| `GET_with_empty_request_and_bare_hostname` | Bare hostname |

**Coverage Summary**:
- URL building: Comprehensive
- Slash handling: Covered
- URL segment replacement: Covered
- Query string building: Not covered (handled in Http class)

---

## 3. Integration Test Coverage

### 3.1 Authentication Tests (`AuthenticationTests.cs`)

**Test Count**: 2 active tests (several commented out)

| Test Name | Status | Coverage |
|-----------|--------|----------|
| `Can_Authenticate_With_Basic_Http_Auth` | Active | HTTP Basic authentication |
| `Can_Authenticate_With_OAuth` | Commented | OAuth 1.0 flow (requires external service) |

**Commented Tests** (require external services):
- `Can_Obtain_OAuth_Request_Token`
- `Can_Obtain_OAuth_Access_Token`
- `Can_Make_Authenticated_OAuth_Call_With_Parameters`
- `Can_Make_Authenticated_OAuth_Call`

**Coverage Gaps**:
- OAuth 2.0 authentication: Not tested
- NTLM authentication: Not tested
- SimpleAuthenticator: Not tested

### 3.2 Async Tests (`AsyncTests.cs`)

**Test Count**: 2 tests

| Test Name | Coverage |
|-----------|----------|
| `Can_Perform_GET_Async` | Async GET with callback |
| `Can_Perform_GET_Async_Without_Async_Handle` | Async GET without handle |

**Coverage Gaps**:
- Async POST/PUT/DELETE: Not tested
- Async with deserialization: Not tested
- Async cancellation: Not tested
- Async timeout handling: Not tested

### 3.3 Compression Tests (`CompressionTests.cs`)

**Test Count**: 3 tests

| Test Name | Coverage |
|-----------|----------|
| `Can_Handle_Gzip_Compressed_Content` | GZip decompression |
| `Can_Handle_Deflate_Compressed_Content` | Deflate decompression |
| `Can_Handle_Uncompressed_Content` | Uncompressed response |

**Coverage Summary**: Compression handling is well covered.

### 3.4 File Tests (`FileTests.cs`)

**Test Count**: 1 test

| Test Name | Coverage |
|-----------|----------|
| `Handles_Binary_File_Download` | Binary file download |

**Coverage Gaps**:
- File upload: Not tested
- Multipart form data: Not tested
- Large file handling: Not tested

### 3.5 Status Code Tests (`StatusCodeTests.cs`)

Tests for HTTP status code handling (file not fully analyzed but exists).

### 3.6 OAuth 1.0 Tests (`oAuth1Tests.cs`)

OAuth 1.0 specific tests (file not fully analyzed but exists).

---

## 4. Coverage Gap Analysis

### Critical Gaps (High Priority)

| Area | Gap | Impact |
|------|-----|--------|
| **Synchronous Operations** | No tests for sync Execute() | Core functionality untested |
| **HTTP Methods** | Only GET tested in integration | POST/PUT/DELETE untested |
| **File Upload** | No upload tests | File operations partially tested |
| **OAuth 2.0** | No OAuth 2.0 tests | Authentication gap |
| **NTLM Auth** | No NTLM tests | Windows auth untested |
| **Error Handling** | Limited error scenario tests | Edge cases untested |
| **Timeout Handling** | No timeout tests | Reliability untested |

### Moderate Gaps (Medium Priority)

| Area | Gap | Impact |
|------|-----|--------|
| **JSON Serialization** | Only XML serialization tested | JSON output untested |
| **Request Body** | Limited body serialization tests | Request building gaps |
| **Headers** | No custom header tests | Header handling untested |
| **Cookies** | No cookie tests | Cookie management untested |
| **Proxy** | No proxy tests | Network config untested |
| **Client Certificates** | No certificate tests | Security features untested |

### Minor Gaps (Low Priority)

| Area | Gap | Impact |
|------|-----|--------|
| **RestClientExtensions** | No extension method tests | Convenience methods untested |
| **Parameter Types** | Limited parameter type tests | Some param types untested |
| **Content Types** | Limited content type tests | MIME handling gaps |

---

## 5. Recommended Test Additions

### 5.1 Authentication Tests

```csharp
// OAuth 2.0 Tests
[Fact] public void Can_Authenticate_With_OAuth2_Bearer_Token()
[Fact] public void Can_Authenticate_With_OAuth2_Query_Parameter()

// NTLM Tests (Framework only)
[Fact] public void Can_Authenticate_With_NTLM()

// Simple Auth Tests
[Fact] public void Can_Authenticate_With_SimpleAuthenticator()
```

### 5.2 HTTP Method Tests

```csharp
// Synchronous Methods
[Fact] public void Can_Perform_Sync_GET()
[Fact] public void Can_Perform_Sync_POST()
[Fact] public void Can_Perform_Sync_PUT()
[Fact] public void Can_Perform_Sync_DELETE()
[Fact] public void Can_Perform_Sync_PATCH()

// Async Methods
[Fact] public void Can_Perform_Async_POST()
[Fact] public void Can_Perform_Async_PUT()
[Fact] public void Can_Perform_Async_DELETE()
```

### 5.3 File Operation Tests

```csharp
[Fact] public void Can_Upload_File_From_Path()
[Fact] public void Can_Upload_File_From_Bytes()
[Fact] public void Can_Upload_Multiple_Files()
[Fact] public void Can_Upload_File_With_Form_Data()
```

### 5.4 Error Handling Tests

```csharp
[Fact] public void Handles_Connection_Timeout()
[Fact] public void Handles_Read_Timeout()
[Fact] public void Handles_404_Not_Found()
[Fact] public void Handles_500_Server_Error()
[Fact] public void Handles_Network_Error()
[Fact] public void Can_Cancel_Async_Request()
```

### 5.5 Serialization Tests

```csharp
// JSON Serialization
[Fact] public void Can_Serialize_Object_To_Json()
[Fact] public void Can_Serialize_With_Custom_JsonSerializer()

// Request Body
[Fact] public void Can_Add_Json_Body()
[Fact] public void Can_Add_Xml_Body()
[Fact] public void Can_Add_Raw_Body()
```

### 5.6 Configuration Tests

```csharp
// Proxy
[Fact] public void Can_Configure_Proxy()

// Certificates
[Fact] public void Can_Add_Client_Certificate()

// Redirects
[Fact] public void Follows_Redirects_When_Enabled()
[Fact] public void Does_Not_Follow_Redirects_When_Disabled()
[Fact] public void Respects_Max_Redirects()
```

---

## 6. Migration Testing Strategy

### 6.1 Pre-Migration Baseline

Before starting migration:

1. **Run All Existing Tests**
   - Document pass/fail status
   - Record execution times
   - Note any flaky tests

2. **Add Missing Critical Tests**
   - Synchronous operation tests
   - All HTTP method tests
   - Error handling tests

3. **Create Compatibility Tests**
   - Tests that verify API signatures
   - Tests that verify behavior contracts

### 6.2 During Migration

1. **Maintain Test Parity**
   - All existing tests must pass
   - New tests for new implementations

2. **Add Platform-Specific Tests**
   - .NET Core specific features
   - HttpClient behavior tests

3. **Performance Comparison Tests**
   - Benchmark critical paths
   - Compare with baseline

### 6.3 Post-Migration Validation

1. **Full Regression Suite**
   - All unit tests pass
   - All integration tests pass

2. **API Compatibility Verification**
   - Public API unchanged
   - Behavior unchanged

3. **Performance Validation**
   - No significant regression
   - Document improvements

### 6.4 Test Infrastructure Updates

For .NET Core migration:

```xml
<!-- Update test project to multi-target -->
<TargetFrameworks>net48;netcoreapp3.1;net6.0</TargetFrameworks>

<!-- Update test dependencies -->
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
```

### 6.5 Continuous Integration

Recommended CI test matrix:

| Framework | OS | Priority |
|-----------|-----|----------|
| .NET Framework 4.8 | Windows | High |
| .NET Core 3.1 | Windows, Linux | High |
| .NET 6.0 | Windows, Linux, macOS | High |
| .NET 7.0+ | Windows, Linux, macOS | Medium |

---

## Test Metrics Summary

### Current State

| Metric | Value |
|--------|-------|
| Total Unit Tests | ~90 |
| Total Integration Tests | ~10 |
| Estimated Code Coverage | 40-50% |
| Test Frameworks | xUnit |

### Target State (Post-Migration)

| Metric | Target |
|--------|--------|
| Total Unit Tests | 150+ |
| Total Integration Tests | 30+ |
| Target Code Coverage | 80%+ |
| Test Frameworks | xUnit |

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
