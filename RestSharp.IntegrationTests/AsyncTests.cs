using System.Net;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class AsyncTests
{
    [Test]
    public async Task Can_Perform_GET_Async()
    {
        const string baseUrl = "http://localhost:8888/";
        const string val = "Basic async test";
        using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("");

            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.Not.Null);
            Assert.That(response.Content, Is.EqualTo(val));
        }
    }

    [Test]
    public async Task Can_Perform_POST_Async()
    {
        const string baseUrl = "http://localhost:8889/";
        using (SimpleServer.Create(baseUrl, Handlers.Echo))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("", Method.Post);
            request.AddStringBody("test body content", ContentType.Plain);

            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.Not.Null);
            Assert.That(response.Content, Is.EqualTo("test body content"));
        }
    }

    [Test]
    public async Task Can_Perform_GET_Async_With_Response_Type()
    {
        const string baseUrl = "http://localhost:8890/";
        const string json = "{\"Message\":\"Hello World\"}";
        using (SimpleServer.Create(baseUrl, ctx =>
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.OutputStream.WriteStringUtf8(json);
        }))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("");

            var response = await client.ExecuteAsync<SimpleResponse>(request);

            Assert.That(response.Data, Is.Not.Null);
            Assert.That(response.Data!.Message, Is.EqualTo("Hello World"));
        }
    }

    public class SimpleResponse
    {
        public string? Message { get; set; }
    }
}
