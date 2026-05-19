using System;
using System.Linq;
using System.Text;
using Xunit;

namespace RestSharp.Tests
{
	public class AuthenticatorTests
	{
		[Fact]
		public void HttpBasicAuthenticator_Adds_Authorization_Header()
		{
			var auth = new HttpBasicAuthenticator("user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);

			var param = request.Parameters.FirstOrDefault(p => p.Name == "Authorization");
			Assert.NotNull(param);
			Assert.Equal(ParameterType.HttpHeader, param.Type);

			var expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
			Assert.Equal("Basic " + expectedToken, param.Value);
		}

		[Fact]
		public void HttpBasicAuthenticator_Does_Not_Duplicate()
		{
			var auth = new HttpBasicAuthenticator("user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);
			auth.Authenticate(client, request);

			var count = request.Parameters.Count(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.Equal(1, count);
		}

		[Fact]
		public void NtlmAuthenticator_Sets_Default_Credentials()
		{
			var auth = new NtlmAuthenticator();
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);

			Assert.Equal(System.Net.CredentialCache.DefaultCredentials, request.Credentials);
		}

		[Fact]
		public void SimpleAuthenticator_Adds_Username_And_Password_Parameters()
		{
			var auth = new SimpleAuthenticator("user_key", "admin", "pass_key", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);

			var userParam = request.Parameters.FirstOrDefault(p => p.Name == "user_key");
			Assert.NotNull(userParam);
			Assert.Equal("admin", userParam.Value);
			Assert.Equal(ParameterType.GetOrPost, userParam.Type);

			var passParam = request.Parameters.FirstOrDefault(p => p.Name == "pass_key");
			Assert.NotNull(passParam);
			Assert.Equal("secret", passParam.Value);
			Assert.Equal(ParameterType.GetOrPost, passParam.Type);
		}

		[Fact]
		public void OAuth2UriQueryParameterAuthenticator_Adds_Token()
		{
			var auth = new OAuth2UriQueryParameterAuthenticator("mytoken123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);

			var param = request.Parameters.FirstOrDefault(p => p.Name == "oauth_token");
			Assert.NotNull(param);
			Assert.Equal("mytoken123", param.Value);
			Assert.Equal(ParameterType.GetOrPost, param.Type);
		}

		[Fact]
		public void OAuth2AuthorizationRequestHeaderAuthenticator_Adds_Header()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("mytoken123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);

			var param = request.Parameters.FirstOrDefault(p => p.Name == "Authorization");
			Assert.NotNull(param);
			Assert.Equal("OAuth mytoken123", param.Value);
			Assert.Equal(ParameterType.HttpHeader, param.Type);
		}

		[Fact]
		public void OAuth2AuthorizationRequestHeaderAuthenticator_Does_Not_Duplicate()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("mytoken123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");

			auth.Authenticate(client, request);
			auth.Authenticate(client, request);

			var count = request.Parameters.Count(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.Equal(1, count);
		}
	}
}
