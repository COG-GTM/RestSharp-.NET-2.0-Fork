# RestSharp .NET Core - Simple .NET REST Client

This is a modernized version of RestSharp migrated from .NET Framework to .NET Core.

**Target Frameworks:** .NET 8.0 and .NET Standard 2.0

**Migration Notes:** This version has been completely migrated from .NET Framework to .NET Core, with HttpClient replacing HttpWebRequest, modern async/await patterns, and removal of legacy platform support.

### [Official Site/Blog][1] - [@RestSharp][2]  
### Please use the [Google Group][3] for feature requests and troubleshooting usage.
### License: Apache License 2.0  

### Features

* Supports .NET 8.0 and .NET Standard 2.0
* Modern HttpClient-based HTTP infrastructure
* Automatic XML and JSON deserialization with Newtonsoft.Json 13.0.3
* Supports custom serialization and deserialization via ISerializer and IDeserializer
* Fuzzy element name matching ('product_id' in XML/JSON will match C# property named 'ProductId')
* Automatic detection of type of content returned
* GET, POST, PUT, HEAD, OPTIONS, DELETE supported
* oAuth 1, oAuth 2, Basic, NTLM and Parameter-based Authenticators included
* Supports custom authentication schemes via IAuthenticator
* Multi-part form/file uploads
* Modern async/await patterns with cancellation token support
* Cross-platform compatibility (.NET Core, .NET Framework via .NET Standard 2.0)

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
RestResponse response = client.Execute(request);
var content = response.Content; // raw content as string

// or automatically deserialize result
// return content type is sniffed but can be explicitly set via RestClient.AddHandler();
RestResponse<Person> response2 = client.Execute<Person>(request);
var name = response2.Data.Name;

// or download and save file to disk
client.DownloadData(request).SaveAs(path);

// easy async support
client.ExecuteAsync(request, response => {
    Console.WriteLine(response.Content);
});

// async with deserialization
var asyncHandle = client.ExecuteAsync<Person>(request, response => {
    Console.WriteLine(response.Data.Name);
});

// abort the request on demand
asyncHandle.Abort();
```
 
  [1]: http://restsharp.org
  [2]: http://twitter.com/RestSharp
  [3]: http://groups.google.com/group/RestSharp
