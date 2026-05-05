using System;
using System.Linq;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth;
using Xunit;

namespace RestSharp.Tests
{
	public class OAuth1AuthenticatorTests
	{
		[Fact]
		public void ForRequestToken_Creates_Authenticator()
		{
			var auth = OAuth1Authenticator.ForRequestToken("key", "secret");
			Assert.NotNull(auth);
			Assert.Equal(OAuthParameterHandling.HttpAuthorizationHeader, auth.ParameterHandling);
			Assert.Equal(OAuthSignatureMethod.HmacSha1, auth.SignatureMethod);
			Assert.Equal(OAuthSignatureTreatment.Escaped, auth.SignatureTreatment);
		}

		[Fact]
		public void ForRequestToken_With_Callback()
		{
			var auth = OAuth1Authenticator.ForRequestToken("key", "secret", "http://callback.com");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForAccessToken_Creates_Authenticator()
		{
			var auth = OAuth1Authenticator.ForAccessToken("key", "secret", "token", "token_secret");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForAccessToken_With_Verifier()
		{
			var auth = OAuth1Authenticator.ForAccessToken("key", "secret", "token", "token_secret", "verifier");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForAccessTokenRefresh()
		{
			var auth = OAuth1Authenticator.ForAccessTokenRefresh("key", "secret", "token", "token_secret", "session");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForAccessTokenRefresh_With_Verifier()
		{
			var auth = OAuth1Authenticator.ForAccessTokenRefresh("key", "secret", "token", "token_secret", "verifier", "session");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForClientAuthentication_Creates_Authenticator()
		{
			var auth = OAuth1Authenticator.ForClientAuthentication("key", "secret", "user", "pass");
			Assert.NotNull(auth);
		}

		[Fact]
		public void ForProtectedResource_Creates_Authenticator()
		{
			var auth = OAuth1Authenticator.ForProtectedResource("key", "secret", "access_token", "access_secret");
			Assert.NotNull(auth);
		}

		[Fact]
		public void Authenticate_RequestToken_Adds_Authorization_Header()
		{
			var auth = OAuth1Authenticator.ForRequestToken("key", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/oauth/request_token");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
			Assert.StartsWith("OAuth ", header.Value.ToString());
		}

		[Fact]
		public void Authenticate_AccessToken_Adds_Authorization_Header()
		{
			var auth = OAuth1Authenticator.ForAccessToken("key", "secret", "request_token", "request_secret", "verifier");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/oauth/access_token");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
			Assert.Contains("oauth_", header.Value.ToString());
		}

		[Fact]
		public void Authenticate_ProtectedResource_Adds_Authorization_Header()
		{
			var auth = OAuth1Authenticator.ForProtectedResource("key", "secret", "access_token", "access_secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api/resource");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
		}

		[Fact]
		public void Authenticate_ClientAuth_Adds_Authorization_Header()
		{
			var auth = OAuth1Authenticator.ForClientAuthentication("key", "secret", "user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/oauth/access_token");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
		}

		[Fact]
		public void Authenticate_With_Realm()
		{
			var auth = OAuth1Authenticator.ForRequestToken("key", "secret");
			auth.Realm = "example_realm";
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/oauth/request_token");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.Contains("realm=", header.Value.ToString());
		}

		[Fact]
		public void Authenticate_UrlOrPostParameters_Mode()
		{
			var auth = OAuth1Authenticator.ForRequestToken("key", "secret");
			auth.ParameterHandling = OAuthParameterHandling.UrlOrPostParameters;
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/oauth/request_token");

			auth.Authenticate(client, request);

			var oauthParams = request.Parameters.Where(
				p => p.Name.StartsWith("oauth_") && p.Type == ParameterType.GetOrPost).ToList();
			Assert.NotEmpty(oauthParams);
		}

		[Fact]
		public void Authenticate_POST_With_Body_Parameters()
		{
			var auth = OAuth1Authenticator.ForProtectedResource("key", "secret", "token", "token_secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api/resource", Method.POST);
			request.AddParameter("status", "Hello World");

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
		}
	}
}
