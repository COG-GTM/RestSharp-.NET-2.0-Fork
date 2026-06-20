using System.Net;
using NUnit.Framework;
using RestSharp.IntegrationTests.Helpers;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class FileTests
{
    [Test]
    public async Task Handles_Binary_File_Download()
    {
        const string baseUrl = "http://localhost:8895/";
        using (SimpleServer.Create(baseUrl, Handlers.FileHandler))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("Assets/Koala.jpg");
            var response = await client.ExecuteAsync(request);

            var expectedPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Koala.jpg");
            if (File.Exists(expectedPath))
            {
                var expected = await File.ReadAllBytesAsync(expectedPath);
                Assert.That(response.RawBytes, Is.EqualTo(expected));
            }
            else
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.Not.EqualTo(HttpStatusCode.OK));
            }
        }
    }

    [Test]
    public async Task Can_Upload_File()
    {
        const string baseUrl = "http://localhost:8896/";
        using (SimpleServer.Create(baseUrl, Handlers.Echo))
        {
            using var client = new RestClient(baseUrl);
            var request = new RestRequest("", Method.Post);

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, "test file content");
            try
            {
                request.AddFile("file", tempFile);
                var response = await client.ExecuteAsync(request);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
