using System.Net;
using NUnit.Framework;
using RestSharp.Authenticators;

namespace RestSharp.IntegrationTests;

[TestFixture]
public class oAuth1Tests
{
    [Test]
    [Ignore("Provide your own consumer key/secret before running")]
    public async Task Can_Authenticate_With_OAuth()
    {
        const string consumerKey = "";
        const string consumerSecret = "";

        var options = new RestClientOptions("http://api.twitter.com")
        {
            Authenticator = OAuth1Authenticator.ForRequestToken(consumerKey, consumerSecret)
        };
        using var client = new RestClient(options);
        var request = new RestRequest("oauth/request_token", Method.Post);
        var response = await client.ExecuteAsync(request);

        Assert.That(response, Is.Not.Null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void Can_Create_OAuth1_Authenticator_For_Request_Token()
    {
        var authenticator = OAuth1Authenticator.ForRequestToken("consumerKey", "consumerSecret");

        Assert.That(authenticator, Is.Not.Null);
    }

    [Test]
    public void Can_Create_OAuth1_Authenticator_For_Access_Token()
    {
        var authenticator = OAuth1Authenticator.ForAccessToken(
            "consumerKey", "consumerSecret", "token", "tokenSecret");

        Assert.That(authenticator, Is.Not.Null);
    }

    [Test]
    public void Can_Create_OAuth1_Authenticator_For_Protected_Resource()
    {
        var authenticator = OAuth1Authenticator.ForProtectedResource(
            "consumerKey", "consumerSecret", "accessToken", "accessTokenSecret");

        Assert.That(authenticator, Is.Not.Null);
    }
}
