using System.Net;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;
using RestSharp.Serializers.Xml;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class StatusCodeTests
{
    [Test]
    public async Task Handles_GET_Request_404_Error()
    {
        const string baseUrl = "http://localhost:8897/";
        using (SimpleServer.Create(baseUrl, UrlToStatusCodeHandler))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("404");
            var response = await client.ExecuteAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }

    private static void UrlToStatusCodeHandler(HttpListenerContext obj)
    {
        obj.Response.StatusCode = int.Parse(obj.Request.Url!.Segments.Last());
    }

    [Test]
    public async Task Handles_Non_Existent_Domain()
    {
        using var client = new RestClient("http://nonexistantdomainimguessing.org");
        var request = new RestRequest("foo");
        var response = await client.ExecuteAsync(request);

        Assert.That(response.ResponseStatus, Is.EqualTo(ResponseStatus.Error));
    }

    [Test]
    public async Task Handles_Different_Root_Element_On_Error()
    {
        const string baseUrl = "http://localhost:8898/";
        using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
        {
            using var client = new RestClient(baseUrl, configureSerialization: s => s.UseXmlSerializer());
            var request = new RestRequest("error");
            request.RootElement = "Error";

            var response = await client.ExecuteAsync<Response>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Message, Is.EqualTo("Not found!"));
        }
    }

    [Test]
    public async Task Handles_Default_Root_Element_On_No_Error()
    {
        const string baseUrl = "http://localhost:8899/";
        using (SimpleServer.Create(baseUrl, Handlers.Generic<ResponseHandler>()))
        {
            using var client = new RestClient(baseUrl, configureSerialization: s => s.UseXmlSerializer());
            var request = new RestRequest("success");
            request.RootElement = "Success";

            var response = await client.ExecuteAsync<Response>(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Message, Is.EqualTo("Works!"));
        }
    }
}

public class ResponseHandler
{
    void error(HttpListenerContext context)
    {
        context.Response.StatusCode = 400;
        context.Response.Headers.Add("Content-Type", "application/xml");
        context.Response.OutputStream.WriteStringUtf8(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Response>
    <Error>
        <Message>Not found!</Message>
    </Error>
</Response>");
    }

    void success(HttpListenerContext context)
    {
        context.Response.Headers.Add("Content-Type", "application/xml");
        context.Response.OutputStream.WriteStringUtf8(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Response>
    <Success>
        <Message>Works!</Message>
    </Success>
</Response>");
    }
}

public class Response
{
    public string? Message { get; set; }
}
