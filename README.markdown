# RestSharp - Simple .NET REST Client

A modern .NET 8 REST and HTTP client library, consuming [RestSharp](https://www.nuget.org/packages/RestSharp) via NuGet.

### License: Apache License 2.0

### Features

* Targets .NET 8
* Automatic XML and JSON deserialization
* Supports custom serialization and deserialization via ISerializer and IDeserializer
* Fuzzy element name matching ('product_id' in XML/JSON will match C# property named 'ProductId')
* Automatic detection of type of content returned
* GET, POST, PUT, HEAD, OPTIONS, DELETE supported
* OAuth 1, OAuth 2, Basic, NTLM and Parameter-based Authenticators included
* Supports custom authentication schemes via IAuthenticator
* Multi-part form/file uploads
* Async/await support throughout

### Getting Started

```bash
dotnet restore
dotnet build
dotnet test
```

### Usage

```csharp
var client = new RestClient("https://example.com");

// Simple GET with async/await
var request = new RestRequest("resource/{id}");
request.AddUrlSegment("id", 123);
var response = await client.GetAsync<MyResource>(request);

// POST with body
var postRequest = new RestRequest("resource", Method.Post);
postRequest.AddJsonBody(new { Name = "value", Id = 123 });
var postResponse = await client.PostAsync<MyResource>(postRequest);

// Using shorthand methods
var resource = await client.GetJsonAsync<MyResource>("resource/123");

// Adding headers and parameters
var request2 = new RestRequest("resource");
request2.AddHeader("X-Custom-Header", "value");
request2.AddQueryParameter("filter", "active");
var result = await client.ExecuteAsync<List<MyResource>>(request2);
```

### Project Structure

- `RestSharp/` — Main project (references RestSharp NuGet package)
- `RestSharp.Tests/` — Unit tests (NUnit)
- `RestSharp.IntegrationTests/` — Integration tests (NUnit)

### Links

- [RestSharp on NuGet](https://www.nuget.org/packages/RestSharp)
- [RestSharp Documentation](https://restsharp.dev)
