using System.Net;
using System.Text;
using NUnit.Framework;
using RestSharp.Authenticators;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class AuthenticationTests
{
    [Test]
    public async Task Can_Authenticate_With_Basic_Http_Auth()
    {
        const string baseUrl = "http://localhost:8891/";
        using (SimpleServer.Create(baseUrl, UsernamePasswordEchoHandler))
        {
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new HttpBasicAuthenticator("testuser", "testpassword")
            };
            using var client = new RestClient(options);

            var request = new RestRequest("test");
            var response = await client.ExecuteAsync(request);

            Assert.That(response.Content, Is.EqualTo("testuser|testpassword"));
        }
    }

    private static void UsernamePasswordEchoHandler(HttpListenerContext context)
    {
        var header = context.Request.Headers["Authorization"];

        var parts = Encoding.ASCII
            .GetString(Convert.FromBase64String(header!.Substring("Basic ".Length)))
            .Split(':');
        context.Response.OutputStream.WriteStringUtf8(string.Join("|", parts));
    }
}
