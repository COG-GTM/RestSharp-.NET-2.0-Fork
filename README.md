# RestSharp - Simple .NET REST Client

License: Apache License 2.0

This is a modernized fork of the original RestSharp 102.x source. The dead platform
targets (Silverlight, Windows Phone 7/7.1, MonoTouch, MonoDroid and the hand-maintained
.NET 2.0 fork) have been removed, the transport is built on `HttpClient`, and the
package is produced with `dotnet pack`.

### Supported frameworks

* `netstandard2.0`
* `net8.0`

### Features

* Automatic XML and JSON deserialization
* Supports custom serialization and deserialization via ISerializer and IDeserializer
* Fuzzy element name matching ('product_id' in XML/JSON will match C# property named 'ProductId')
* Automatic detection of type of content returned
* GET, POST, PUT, PATCH, HEAD, OPTIONS, DELETE supported
* oAuth 1, oAuth 2, Basic, NTLM and Parameter-based Authenticators included
* Supports custom authentication schemes via IAuthenticator
* Multi-part form/file uploads
* T4 Helper to generate C# classes from an XML document

```csharp
var client = new RestClient("http://example.com");
// client.Authenticator = new HttpBasicAuthenticator(username, password);

var request = new RestRequest("resource/{id}", Method.POST);
request.AddParameter("name", "value"); // adds to POST or URL querystring based on Method
request.AddUrlSegment("id", 123); // replaces matching token in request.Resource

// add parameters for all properties on an object
request.AddObject(object);

// or just whitelisted properties
request.AddObject(object, "PersonId", "Name", ...);

// easily add HTTP Headers
request.AddHeader("header", "value");

// add files to upload (works with compatible verbs)
request.AddFile(path);

// execute the request
RestResponse response = await client.ExecuteAsync(request);
var content = response.Content; // raw content as string

// or automatically deserialize result
// return content type is sniffed but can be explicitly set via RestClient.AddHandler();
RestResponse<Person> response2 = await client.ExecuteAsync<Person>(request);
var name = response2.Data.Name;

// or download and save file to disk
(await client.DownloadDataAsync(request)).SaveAs(path);

// cancel a request with a CancellationToken
var cancellation = new CancellationTokenSource();
var task = client.ExecuteAsync(request, cancellation.Token);
cancellation.Cancel(); // the response comes back with ResponseStatus.Aborted

// synchronous overloads are still available
RestResponse sync = client.Execute(request);
```

### Building

```bash
dotnet build RestSharp.sln
dotnet test RestSharp.sln
dotnet pack RestSharp/RestSharp.csproj --configuration Release --output artifacts
```
