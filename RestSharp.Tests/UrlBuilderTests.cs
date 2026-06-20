using NUnit.Framework;
using RestSharp;

namespace RestSharp.Tests;

[TestFixture]
public class UrlBuilderTests
{
    [Test]
    public void GET_with_leading_slash()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("/resource");

        var expected = new Uri("http://example.com/resource");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void POST_with_leading_slash()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("/resource", Method.Post);

        var expected = new Uri("http://example.com/resource");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_leading_slash_and_baseurl_trailing_slash()
    {
        var options = new RestClientOptions("http://example.com/");
        using var client = new RestClient(options);
        var request = new RestRequest("/resource");
        request.AddParameter("foo", "bar", ParameterType.QueryString);

        var expected = new Uri("http://example.com/resource?foo=bar");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void POST_with_leading_slash_and_baseurl_trailing_slash()
    {
        var options = new RestClientOptions("http://example.com/");
        using var client = new RestClient(options);
        var request = new RestRequest("/resource", Method.Post);

        var expected = new Uri("http://example.com/resource");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_resource_containing_slashes()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("resource/foo");

        var expected = new Uri("http://example.com/resource/foo");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void POST_with_resource_containing_slashes()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("resource/foo", Method.Post);

        var expected = new Uri("http://example.com/resource/foo");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_resource_containing_tokens()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("resource/{foo}");
        request.AddUrlSegment("foo", "bar");

        var expected = new Uri("http://example.com/resource/bar");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void POST_with_resource_containing_tokens()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("resource/{foo}", Method.Post);
        request.AddUrlSegment("foo", "bar");

        var expected = new Uri("http://example.com/resource/bar");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_empty_request()
    {
        var options = new RestClientOptions("http://example.com/resource");
        using var client = new RestClient(options);
        var request = new RestRequest();

        var expected = new Uri("http://example.com/resource");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_empty_request_and_bare_hostname()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest();

        var expected = new Uri("http://example.com/");
        var output = client.BuildUri(request);

        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public void GET_with_multiple_query_parameters()
    {
        var options = new RestClientOptions("http://example.com");
        using var client = new RestClient(options);
        var request = new RestRequest("resource");
        request.AddParameter("foo", "bar", ParameterType.QueryString);
        request.AddParameter("baz", "qux", ParameterType.QueryString);

        var output = client.BuildUri(request);

        Assert.That(output.ToString(), Does.Contain("foo=bar"));
        Assert.That(output.ToString(), Does.Contain("baz=qux"));
    }
}
