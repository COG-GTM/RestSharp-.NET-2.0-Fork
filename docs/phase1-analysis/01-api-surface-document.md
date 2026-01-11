# RestSharp API Surface Document

## Phase 1 Migration Analysis - API Inventory

This document provides a complete inventory of all public APIs in the RestSharp library, including method signatures, properties, and current behavior. This inventory serves as the baseline for the .NET Framework to .NET Core migration.

---

## Table of Contents

1. [Core Client APIs](#1-core-client-apis)
2. [Request and Response APIs](#2-request-and-response-apis)
3. [Authentication APIs](#3-authentication-apis)
4. [Serialization APIs](#4-serialization-apis)
5. [Deserialization APIs](#5-deserialization-apis)
6. [HTTP Infrastructure APIs](#6-http-infrastructure-apis)
7. [Supporting Types and Enums](#7-supporting-types-and-enums)

---

## 1. Core Client APIs

### 1.1 IRestClient Interface

**Location**: `RestSharp/IRestClient.cs`

The primary interface for REST client operations.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CookieContainer` | `CookieContainer` | Container for cookies to be sent with requests |
| `UserAgent` | `string` | User agent string sent with requests |
| `Timeout` | `int` | Request timeout in milliseconds |
| `Authenticator` | `IAuthenticator` | Authentication handler for requests |
| `BaseUrl` | `Uri` | Base URL for all requests |
| `DefaultParameters` | `IList<Parameter>` | Default parameters added to all requests |
| `Proxy` | `IWebProxy` | Proxy configuration (FRAMEWORK only) |
| `ClientCertificates` | `X509CertificateCollection` | Client certificates (FRAMEWORK only) |
| `FollowRedirects` | `bool` | Whether to follow HTTP redirects |
| `MaxRedirects` | `int` | Maximum number of redirects to follow (FRAMEWORK only) |

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Execute(IRestRequest request)` | `IRestResponse` | Execute a synchronous request (FRAMEWORK only) |
| `Execute<T>(IRestRequest request)` | `IRestResponse<T>` | Execute and deserialize response (FRAMEWORK only) |
| `ExecuteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)` | `RestRequestAsyncHandle` | Execute asynchronous request with callback |
| `ExecuteAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback)` | `RestRequestAsyncHandle` | Execute async and deserialize with callback |
| `BuildUri(IRestRequest request)` | `Uri` | Build the full URI for a request |
| `DownloadData(IRestRequest request)` | `byte[]` | Download binary data (FRAMEWORK only) |

### 1.2 RestClient Class

**Location**: `RestSharp/RestClient.cs`, `RestSharp/RestClient.Sync.cs`, `RestSharp/RestClient.Async.cs`

The main implementation of `IRestClient`.

#### Constructors

```csharp
public RestClient()
public RestClient(Uri baseUrl)
public RestClient(string baseUrl)
```

#### Key Implementation Details

- Uses `HttpFactory` to create `IHttp` instances for each request
- Manages content type handlers for serialization/deserialization
- Supports automatic decompression (GZip/Deflate)
- Handles URL building with parameter substitution

#### Usage Example

```csharp
var client = new RestClient("https://api.example.com");
client.Authenticator = new HttpBasicAuthenticator("user", "pass");

var request = new RestRequest("resource/{id}", Method.GET);
request.AddUrlSegment("id", "123");

// Synchronous (FRAMEWORK only)
var response = client.Execute(request);

// Asynchronous (all platforms)
client.ExecuteAsync(request, response => {
    Console.WriteLine(response.Content);
});
```

---

## 2. Request and Response APIs

### 2.1 IRestRequest Interface

**Location**: `RestSharp/IRestRequest.cs`

Interface for configuring HTTP requests.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AlwaysMultipartFormData` | `bool` | Force multipart form data even without files |
| `JsonSerializer` | `ISerializer` | Custom JSON serializer |
| `XmlSerializer` | `ISerializer` | Custom XML serializer |
| `RequestFormat` | `DataFormat` | Format for request body (Json/Xml) |
| `Resource` | `string` | Resource path (appended to base URL) |
| `Method` | `Method` | HTTP method (GET, POST, PUT, DELETE, etc.) |
| `Parameters` | `List<Parameter>` | Request parameters |
| `Files` | `List<FileParameter>` | Files for upload |
| `Timeout` | `int` | Request-specific timeout |
| `Attempts` | `int` | Number of execution attempts |
| `Credentials` | `ICredentials` | Request credentials (FRAMEWORK only) |
| `OnBeforeDeserialization` | `Action<IRestResponse>` | Callback before deserialization |

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `AddParameter(Parameter p)` | `IRestRequest` | Add a parameter |
| `AddParameter(string name, object value)` | `IRestRequest` | Add a GetOrPost parameter |
| `AddParameter(string name, object value, ParameterType type)` | `IRestRequest` | Add a typed parameter |
| `AddFile(string name, string path)` | `IRestRequest` | Add file from path |
| `AddFile(string name, byte[] bytes, string fileName)` | `IRestRequest` | Add file from bytes |
| `AddFile(string name, Action<Stream> writer, string fileName)` | `IRestRequest` | Add file with stream writer |
| `AddBody(object obj)` | `IRestRequest` | Add serialized body |
| `AddBody(object obj, string xmlNamespace)` | `IRestRequest` | Add XML body with namespace |
| `AddObject(object obj)` | `IRestRequest` | Add object properties as parameters |
| `AddObject(object obj, params string[] whitelist)` | `IRestRequest` | Add whitelisted properties |
| `AddUrlSegment(string name, string value)` | `IRestRequest` | Add URL segment replacement |
| `AddHeader(string name, string value)` | `IRestRequest` | Add HTTP header |
| `AddCookie(string name, string value)` | `IRestRequest` | Add cookie |

### 2.2 RestRequest Class

**Location**: `RestSharp/RestRequest.cs`

Implementation of `IRestRequest`.

#### Constructors

```csharp
public RestRequest()
public RestRequest(Method method)
public RestRequest(string resource)
public RestRequest(string resource, Method method)
public RestRequest(Uri resource)
public RestRequest(Uri resource, Method method)
```

#### Usage Example

```csharp
var request = new RestRequest("users/{id}", Method.GET);
request.AddUrlSegment("id", "123");
request.AddHeader("Accept", "application/json");
request.AddParameter("include", "profile");
```

### 2.3 IRestResponse Interface

**Location**: `RestSharp/IRestResponse.cs`

Interface for HTTP response data.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Request` | `IRestRequest` | Original request |
| `ContentType` | `string` | Response content type |
| `ContentLength` | `long` | Response content length |
| `ContentEncoding` | `string` | Response encoding |
| `Content` | `string` | Response body as string |
| `StatusCode` | `HttpStatusCode` | HTTP status code |
| `StatusDescription` | `string` | HTTP status description |
| `RawBytes` | `byte[]` | Raw response bytes |
| `ResponseUri` | `Uri` | Final response URI |
| `Server` | `string` | Server header value |
| `Cookies` | `IList<RestResponseCookie>` | Response cookies |
| `Headers` | `IList<Parameter>` | Response headers |
| `ResponseStatus` | `ResponseStatus` | Request completion status |
| `ErrorMessage` | `string` | Error message if failed |
| `ErrorException` | `Exception` | Exception if failed |

### 2.4 IRestResponse<T> Interface

**Location**: `RestSharp/IRestResponse.cs`

Generic response interface with deserialized data.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `T` | Deserialized response data |

### 2.5 RestResponse Class

**Location**: `RestSharp/RestResponse.cs`

Implementation of `IRestResponse` and `IRestResponse<T>`.

---

## 3. Authentication APIs

### 3.1 IAuthenticator Interface

**Location**: `RestSharp/Authenticators/IAuthenticator.cs`

Base interface for all authenticators.

```csharp
public interface IAuthenticator
{
    void Authenticate(IRestClient client, IRestRequest request);
}
```

### 3.2 HttpBasicAuthenticator

**Location**: `RestSharp/Authenticators/HttpBasicAuthenticator.cs`

HTTP Basic authentication implementation.

#### Constructor

```csharp
public HttpBasicAuthenticator(string username, string password)
```

#### Behavior

- Adds `Authorization` header with Base64-encoded credentials
- Format: `Basic {base64(username:password)}`
- Only adds header if not already present

#### Usage Example

```csharp
client.Authenticator = new HttpBasicAuthenticator("user", "pass");
```

### 3.3 OAuth1Authenticator

**Location**: `RestSharp/Authenticators/OAuth1Authenticator.cs`

OAuth 1.0a authentication implementation.

#### Factory Methods

| Method | Description |
|--------|-------------|
| `ForRequestToken(consumerKey, consumerSecret)` | Create authenticator for request token |
| `ForRequestToken(consumerKey, consumerSecret, callbackUrl)` | Request token with callback |
| `ForAccessToken(consumerKey, consumerSecret, token, tokenSecret)` | Create for access token |
| `ForAccessToken(consumerKey, consumerSecret, token, tokenSecret, verifier)` | Access token with verifier |
| `ForAccessTokenRefresh(consumerKey, consumerSecret, token, tokenSecret, sessionHandle)` | Refresh access token |
| `ForClientAuthentication(consumerKey, consumerSecret, username, password)` | xAuth client authentication |
| `ForProtectedResource(consumerKey, consumerSecret, accessToken, accessTokenSecret)` | Protected resource access |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Realm` | `string` | OAuth realm |
| `ParameterHandling` | `OAuthParameterHandling` | How to pass OAuth params |
| `SignatureMethod` | `OAuthSignatureMethod` | Signature algorithm |
| `SignatureTreatment` | `OAuthSignatureTreatment` | Signature encoding |

#### Usage Example

```csharp
// Request token flow
client.Authenticator = OAuth1Authenticator.ForRequestToken(
    "consumer_key", "consumer_secret"
);

// Protected resource access
client.Authenticator = OAuth1Authenticator.ForProtectedResource(
    "consumer_key", "consumer_secret",
    "access_token", "access_token_secret"
);
```

### 3.4 OAuth2Authenticator (Abstract)

**Location**: `RestSharp/Authenticators/OAuth2Authenticator.cs`

Base class for OAuth 2.0 authentication.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AccessToken` | `string` | OAuth 2.0 access token |

#### Implementations

**OAuth2UriQueryParameterAuthenticator**
- Adds access token as query parameter `oauth_token`

**OAuth2AuthorizationRequestHeaderAuthenticator**
- Adds `Authorization: Bearer {token}` header
- Supports custom token type prefix

#### Usage Example

```csharp
// Query parameter method
client.Authenticator = new OAuth2UriQueryParameterAuthenticator("access_token");

// Header method (recommended)
client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator("access_token", "Bearer");
```

### 3.5 NtlmAuthenticator

**Location**: `RestSharp/Authenticators/NtlmAuthenticator.cs`

**Platform**: FRAMEWORK only

NTLM/Windows authentication using default credentials.

```csharp
public class NtlmAuthenticator : IAuthenticator
{
    public void Authenticate(IRestClient client, IRestRequest request)
    {
        request.Credentials = CredentialCache.DefaultCredentials;
    }
}
```

### 3.6 SimpleAuthenticator

**Location**: `RestSharp/Authenticators/SimpleAuthenticator.cs`

Simple key-value pair authentication.

#### Constructor

```csharp
public SimpleAuthenticator(string usernameKey, string username, string passwordKey, string password)
```

#### Behavior

- Adds username and password as request parameters
- Useful for APIs that accept credentials as form fields

---

## 4. Serialization APIs

### 4.1 ISerializer Interface

**Location**: `RestSharp/Serializers/ISerializer.cs`

Interface for request body serialization.

```csharp
public interface ISerializer
{
    string Serialize(object obj);
    string RootElement { get; set; }
    string Namespace { get; set; }
    string DateFormat { get; set; }
    string ContentType { get; set; }
}
```

### 4.2 JsonSerializer

**Location**: `RestSharp/Serializers/JsonSerializer.cs`

JSON serialization using Newtonsoft.Json.

#### Constructors

```csharp
public JsonSerializer()
public JsonSerializer(Newtonsoft.Json.JsonSerializer serializer)
```

#### Default Configuration

- `MissingMemberHandling`: Ignore
- `NullValueHandling`: Include
- `DefaultValueHandling`: Include
- `ContentType`: "application/json"

#### Usage Example

```csharp
var request = new RestRequest("resource", Method.POST);
request.RequestFormat = DataFormat.Json;
request.AddBody(new { Name = "Test", Value = 123 });
```

### 4.3 XmlSerializer

**Location**: `RestSharp/Serializers/XmlSerializer.cs`

XML serialization using XDocument.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `RootElement` | `string` | Root element name |
| `Namespace` | `string` | XML namespace |
| `DateFormat` | `string` | Date format string |
| `ContentType` | `string` | Content type (default: "text/xml") |

#### Features

- Supports `SerializeAsAttribute` for custom property names
- Handles nested objects and collections
- Supports nullable types

### 4.4 SerializeAsAttribute

**Location**: `RestSharp/Serializers/SerializeAsAttribute.cs`

Attribute for customizing serialization behavior.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Custom element/property name |
| `Attribute` | `bool` | Serialize as XML attribute |
| `Content` | `bool` | Serialize as element content |
| `Culture` | `CultureInfo` | Culture for formatting |
| `NameStyle` | `NameStyle` | Naming convention |
| `Index` | `int` | Serialization order |

### 4.5 DotNetXmlSerializer

**Location**: `RestSharp/Serializers/DotNetXmlSerializer.cs`

XML serialization using .NET's built-in XmlSerializer.

---

## 5. Deserialization APIs

### 5.1 IDeserializer Interface

**Location**: `RestSharp/Deserializers/IDeserializer.cs`

Interface for response deserialization.

```csharp
public interface IDeserializer
{
    T Deserialize<T>(RestResponse response) where T : new();
    string RootElement { get; set; }
    string Namespace { get; set; }
    string DateFormat { get; set; }
}
```

### 5.2 JsonDeserializer

**Location**: `RestSharp/Deserializers/JsonDeserializer.cs`

JSON deserialization using Newtonsoft.Json.Linq.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `RootElement` | `string` | JSON path to root element |
| `Namespace` | `string` | Not used for JSON |
| `DateFormat` | `string` | Custom date format |
| `Culture` | `CultureInfo` | Culture for parsing |

#### Features

- Fuzzy element name matching (exact, lowercase, camelCase, underscored, dashed)
- Handles nullable types
- Supports enums (by name or value)
- DateTime parsing (ISO 8601, Unix timestamps, JavaScript dates)
- Decimal, Guid, Uri, TimeSpan support
- Generic List<T> and Dictionary<string, T> support

#### Usage Example

```csharp
var deserializer = new JsonDeserializer();
deserializer.RootElement = "data";
var result = deserializer.Deserialize<MyClass>(response);
```

### 5.3 XmlDeserializer

**Location**: `RestSharp/Deserializers/XmlDeserializer.cs`

XML deserialization using XDocument.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `RootElement` | `string` | Root element name |
| `Namespace` | `string` | XML namespace |
| `DateFormat` | `string` | Custom date format |
| `Culture` | `CultureInfo` | Culture for parsing |

#### Features

- Fuzzy element name matching
- Handles both elements and attributes
- Supports nested objects and collections
- Namespace handling with removal option

### 5.4 XmlAttributeDeserializer

**Location**: `RestSharp/Deserializers/XmlAttributeDeserializer.cs`

XML deserialization prioritizing attributes over elements.

### 5.5 DeserializeAsAttribute

**Location**: `RestSharp/Deserializers/DeserializeAsAttribute.cs`

Attribute for customizing deserialization behavior.

```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class DeserializeAsAttribute : Attribute
{
    public string Name { get; set; }
}
```

### 5.6 DotNetXmlDeserializer

**Location**: `RestSharp/Deserializers/DotNetXmlDeserializer.cs`

XML deserialization using .NET's built-in XmlSerializer.

---

## 6. HTTP Infrastructure APIs

### 6.1 IHttp Interface

**Location**: `RestSharp/IHttp.cs`

Low-level HTTP interface.

#### Properties

| Property | Type | Platform | Description |
|----------|------|----------|-------------|
| `CookieContainer` | `CookieContainer` | All | Cookie storage |
| `Credentials` | `ICredentials` | All | Request credentials |
| `UserAgent` | `string` | All | User agent string |
| `Timeout` | `int` | All | Timeout in milliseconds |
| `FollowRedirects` | `bool` | !SILVERLIGHT | Follow redirects |
| `ClientCertificates` | `X509CertificateCollection` | FRAMEWORK | Client certificates |
| `MaxRedirects` | `int` | FRAMEWORK | Max redirect count |
| `Headers` | `IList<HttpHeader>` | All | Request headers |
| `Parameters` | `IList<HttpParameter>` | All | Request parameters |
| `Files` | `IList<HttpFile>` | All | Files for upload |
| `Cookies` | `IList<HttpCookie>` | All | Request cookies |
| `RequestBody` | `string` | All | Request body content |
| `RequestContentType` | `string` | All | Content type |
| `Url` | `Uri` | All | Request URL |
| `Proxy` | `IWebProxy` | FRAMEWORK | Proxy configuration |

#### Synchronous Methods (FRAMEWORK only)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `Delete()` | `HttpResponse` | HTTP DELETE |
| `Get()` | `HttpResponse` | HTTP GET |
| `Head()` | `HttpResponse` | HTTP HEAD |
| `Options()` | `HttpResponse` | HTTP OPTIONS |
| `Post()` | `HttpResponse` | HTTP POST |
| `Put()` | `HttpResponse` | HTTP PUT |
| `Patch()` | `HttpResponse` | HTTP PATCH |

#### Asynchronous Methods (All platforms)

| Method | Return Type | Description |
|--------|-------------|-------------|
| `DeleteAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async DELETE |
| `GetAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async GET |
| `HeadAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async HEAD |
| `OptionsAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async OPTIONS |
| `PostAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async POST |
| `PutAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async PUT |
| `PatchAsync(Action<HttpResponse> callback)` | `HttpWebRequest` | Async PATCH |

### 6.2 Http Class

**Location**: `RestSharp/Http.cs`, `RestSharp/Http.Sync.cs`, `RestSharp/Http.Async.cs`

Implementation of `IHttp` using `HttpWebRequest`.

#### Key Implementation Details

- `Http.cs`: Core implementation, property definitions, helper methods
- `Http.Sync.cs`: Synchronous methods (FRAMEWORK only)
- `Http.Async.cs`: Asynchronous methods (all platforms)

#### Factory Method

```csharp
public static Http Create()
```

### 6.3 IHttpResponse Interface

**Location**: `RestSharp/IHttpResponse.cs`

Interface for raw HTTP response data.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ContentType` | `string` | Response content type |
| `ContentLength` | `long` | Response content length |
| `ContentEncoding` | `string` | Content encoding |
| `Content` | `string` | Response body as string |
| `StatusCode` | `HttpStatusCode` | HTTP status code |
| `StatusDescription` | `string` | Status description |
| `RawBytes` | `byte[]` | Raw response bytes |
| `ResponseUri` | `Uri` | Final response URI |
| `Server` | `string` | Server header |
| `Headers` | `IList<HttpHeader>` | Response headers |
| `Cookies` | `IList<HttpCookie>` | Response cookies |
| `ResponseStatus` | `ResponseStatus` | Completion status |
| `ErrorMessage` | `string` | Error message |
| `ErrorException` | `Exception` | Exception if failed |

### 6.4 RestRequestAsyncHandle

**Location**: `RestSharp/RestRequestAsyncHandle.cs`

Handle for canceling async requests.

```csharp
public class RestRequestAsyncHandle
{
    public HttpWebRequest WebRequest;
    
    public RestRequestAsyncHandle()
    public RestRequestAsyncHandle(HttpWebRequest webRequest)
    
    public void Abort()
}
```

### 6.5 IHttpFactory Interface

**Location**: `RestSharp/IHttpFactory.cs`

Factory interface for creating IHttp instances.

```csharp
public interface IHttpFactory
{
    IHttp Create();
}
```

---

## 7. Supporting Types and Enums

### 7.1 ParameterType Enum

**Location**: `RestSharp/Enum.cs`

```csharp
public enum ParameterType
{
    Cookie,      // Sent as cookie
    GetOrPost,   // Query string (GET) or form data (POST)
    UrlSegment,  // URL path segment replacement
    HttpHeader,  // HTTP header
    RequestBody  // Request body content
}
```

### 7.2 Method Enum

**Location**: `RestSharp/Enum.cs`

```csharp
public enum Method
{
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH
}
```

### 7.3 DataFormat Enum

**Location**: `RestSharp/Enum.cs`

```csharp
public enum DataFormat
{
    Json,
    Xml
}
```

### 7.4 ResponseStatus Enum

**Location**: `RestSharp/Enum.cs`

```csharp
public enum ResponseStatus
{
    None,      // Not yet executed
    Completed, // Successfully completed
    Error,     // Error occurred
    TimedOut,  // Request timed out
    Aborted    // Request was aborted
}
```

### 7.5 DateFormat Struct

**Location**: `RestSharp/Enum.cs`

```csharp
public struct DateFormat
{
    public const string Iso8601 = "s";    // ISO 8601 format
    public const string RoundTrip = "u";  // Round-trip format
}
```

### 7.6 Parameter Class

**Location**: `RestSharp/Parameter.cs`

```csharp
public class Parameter
{
    public string Name { get; set; }
    public object Value { get; set; }
    public ParameterType Type { get; set; }
    
    public override string ToString()
}
```

### 7.7 FileParameter Class

**Location**: `RestSharp/FileParameter.cs`

```csharp
public class FileParameter
{
    public long ContentLength { get; set; }
    public Action<Stream> Writer { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string Name { get; set; }
    
    public static FileParameter Create(string name, byte[] data, string filename, string contentType)
    public static FileParameter Create(string name, byte[] data, string filename)
}
```

### 7.8 RestResponseCookie Class

**Location**: `RestSharp/RestResponseCookie.cs`

Represents a cookie from an HTTP response with properties for Name, Value, Comment, CommentUri, Discard, Domain, Expired, Expires, HttpOnly, Path, Port, Secure, TimeStamp, Value, and Version.

### 7.9 HttpHeader, HttpParameter, HttpFile, HttpCookie Classes

**Location**: `RestSharp/HttpHeader.cs`, `RestSharp/HttpParameter.cs`, `RestSharp/HttpFile.cs`, `RestSharp/HttpCookie.cs`

Simple data classes for HTTP request/response components.

---

## Migration Considerations

### APIs Requiring Migration Attention

1. **HttpWebRequest Usage**: The entire HTTP layer uses `HttpWebRequest`, which should be migrated to `HttpClient` in .NET Core.

2. **Synchronous Methods**: All synchronous methods in `Http.Sync.cs` and `RestClient.Sync.cs` are wrapped in `#if FRAMEWORK` and will need async equivalents or Task-based wrappers.

3. **Platform-Specific Features**:
   - `ClientCertificates` (FRAMEWORK only)
   - `Proxy` configuration (FRAMEWORK only)
   - `MaxRedirects` (FRAMEWORK only)
   - `NtlmAuthenticator` (FRAMEWORK only)

4. **Compression Handling**: Windows Phone uses custom ZLib implementation; .NET Core has built-in support.

5. **OAuth Implementation**: Uses platform-specific cryptography that may need updates for .NET Core.

---

*Document generated as part of Phase 1 Migration Analysis*
*Date: January 2026*
