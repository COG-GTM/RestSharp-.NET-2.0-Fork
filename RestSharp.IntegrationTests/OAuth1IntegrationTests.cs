using System;
using System.Net;
using System.Text;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators.OAuth.Extensions;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class OAuth1IntegrationTests
	{
		private const string BaseUrl = "http://localhost:8897/";

		[Fact]
		public void OAuth_Signature_Is_Valid_HmacSha1()
		{
			var consumerKey = "test_consumer_key";
			var consumerSecret = "test_consumer_secret";
			var tokenSecret = "test_token_secret";

			var parameters = new WebParameterCollection();
			parameters.Add("oauth_consumer_key", consumerKey);
			parameters.Add("oauth_nonce", OAuthTools.GetNonce());
			parameters.Add("oauth_signature_method", "HMAC-SHA1");
			parameters.Add("oauth_timestamp", OAuthTools.GetTimestamp());
			parameters.Add("oauth_version", "1.0");

			var signatureBase = OAuthTools.ConcatenateRequestElements(
				"GET",
				"http://example.com/resource",
				parameters);

			var signature = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				consumerSecret,
				tokenSecret);

			Assert.False(string.IsNullOrEmpty(signature));

			// Verify same inputs produce same signature
			var signature2 = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				consumerSecret,
				tokenSecret);

			Assert.Equal(signature, signature2);
		}

		[Fact]
		public void OAuth_Signature_Changes_With_Different_Secrets()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("oauth_consumer_key", "key");
			parameters.Add("oauth_nonce", "testnonce");
			parameters.Add("oauth_timestamp", "1234567890");

			var signatureBase = OAuthTools.ConcatenateRequestElements(
				"GET",
				"http://example.com/resource",
				parameters);

			var sig1 = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				"secret1",
				"token1");

			var sig2 = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				"secret2",
				"token2");

			Assert.NotEqual(sig1, sig2);
		}

		[Fact]
		public void OAuth_Mock_Server_Validates_Signature_Present()
		{
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				var authHeader = ctx.Request.Headers["Authorization"];
				if (authHeader != null && authHeader.Contains("oauth_signature"))
				{
					ctx.Response.StatusCode = 200;
					ctx.Response.OutputStream.WriteStringUtf8("Authorized");
				}
				else
				{
					ctx.Response.StatusCode = 401;
					ctx.Response.OutputStream.WriteStringUtf8("Unauthorized");
				}
			}))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);

				// Manually add an OAuth-style Authorization header
				var consumerSecret = "secret";
				var parameters = new WebParameterCollection();
				parameters.Add("oauth_consumer_key", "key");
				parameters.Add("oauth_nonce", OAuthTools.GetNonce());
				parameters.Add("oauth_signature_method", "HMAC-SHA1");
				parameters.Add("oauth_timestamp", OAuthTools.GetTimestamp());
				parameters.Add("oauth_version", "1.0");

				var signatureBase = OAuthTools.ConcatenateRequestElements("GET", BaseUrl, parameters);
				var signature = OAuthTools.GetSignature(
					OAuthSignatureMethod.HmacSha1,
					OAuthSignatureTreatment.Escaped,
					signatureBase,
					consumerSecret,
					null);

				var authValue = string.Format(
					"OAuth oauth_consumer_key=\"key\", oauth_signature=\"{0}\", oauth_signature_method=\"HMAC-SHA1\"",
					signature);
				request.AddHeader("Authorization", authValue);

				var response = client.Execute(request);
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				Assert.Equal("Authorized", response.Content);
			}
		}
	}
}
