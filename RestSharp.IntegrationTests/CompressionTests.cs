using System.IO.Compression;
using System.Net;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class CompressionTests
{
    [Test]
    public async Task Can_Handle_Gzip_Compressed_Content()
    {
        const string baseUrl = "http://localhost:8892/";
        using (SimpleServer.Create(baseUrl, GzipEchoValue("This is some gzipped content")))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("");
            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("This is some gzipped content"));
        }
    }

    [Test]
    public async Task Can_Handle_Deflate_Compressed_Content()
    {
        const string baseUrl = "http://localhost:8893/";
        using (SimpleServer.Create(baseUrl, DeflateEchoValue("This is some deflated content")))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("");
            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("This is some deflated content"));
        }
    }

    [Test]
    public async Task Can_Handle_Uncompressed_Content()
    {
        const string baseUrl = "http://localhost:8894/";
        using (SimpleServer.Create(baseUrl, Handlers.EchoValue("This is some sample content")))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("");
            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("This is some sample content"));
        }
    }

    private static Action<HttpListenerContext> GzipEchoValue(string value)
    {
        return context =>
        {
            context.Response.Headers.Add("Content-encoding", "gzip");
            using var gzip = new GZipStream(context.Response.OutputStream, CompressionMode.Compress, true);
            gzip.WriteStringUtf8(value);
        };
    }

    private static Action<HttpListenerContext> DeflateEchoValue(string value)
    {
        return context =>
        {
            context.Response.Headers.Add("Content-encoding", "deflate");
            using var deflate = new DeflateStream(context.Response.OutputStream, CompressionMode.Compress, true);
            deflate.WriteStringUtf8(value);
        };
    }
}
