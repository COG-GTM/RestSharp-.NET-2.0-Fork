using System;
using System.Linq;
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
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
			Assert.StartsWith("Basic ", header.Value.ToString());
		}

		[Fact]
		public void HttpBasicAuthenticator_Encodes_Credentials_Correctly()
		{
			var auth = new HttpBasicAuthenticator("admin", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.First(p => p.Name == "Authorization");
			var expectedToken = Convert.ToBase64String(
				System.Text.Encoding.UTF8.GetBytes("admin:secret"));
			Assert.Equal("Basic " + expectedToken, header.Value.ToString());
		}

		[Fact]
		public void HttpBasicAuthenticator_Does_Not_Add_Duplicate()
		{
			var auth = new HttpBasicAuthenticator("user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);
			auth.Authenticate(client, request);

			var headers = request.Parameters
				.Where(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
				.ToList();
			Assert.Single(headers);
		}

		[Fact]
		public void SimpleAuthenticator_Adds_Username_And_Password_Parameters()
		{
			var auth = new SimpleAuthenticator("username", "admin", "password", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			Assert.Equal(2, request.Parameters.Count);
			Assert.Contains(request.Parameters, p => p.Name == "username" && p.Value.ToString() == "admin");
			Assert.Contains(request.Parameters, p => p.Name == "password" && p.Value.ToString() == "secret");
		}

		[Fact]
		public void OAuth2UriQueryParameterAuthenticator_Adds_Token_Parameter()
		{
			var auth = new OAuth2UriQueryParameterAuthenticator("mytoken123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var param = request.Parameters.FirstOrDefault(
				p => p.Name == "oauth_token" && p.Type == ParameterType.GetOrPost);
			Assert.NotNull(param);
			Assert.Equal("mytoken123", param.Value.ToString());
		}

		[Fact]
		public void OAuth2UriQueryParameterAuthenticator_AccessToken_Property()
		{
			var auth = new OAuth2UriQueryParameterAuthenticator("token456");
			Assert.Equal("token456", auth.AccessToken);
		}

		[Fact]
		public void OAuth2AuthorizationRequestHeaderAuthenticator_Adds_Header()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("bearertoken");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(header);
			Assert.Equal("OAuth bearertoken", header.Value.ToString());
		}

		[Fact]
		public void OAuth2AuthorizationRequestHeaderAuthenticator_Does_Not_Add_Duplicate()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("token");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);
			auth.Authenticate(client, request);

			var headers = request.Parameters
				.Where(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
				.ToList();
			Assert.Single(headers);
		}

		[Fact]
		public void NtlmAuthenticator_Sets_DefaultCredentials()
		{
			var auth = new NtlmAuthenticator();
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			Assert.NotNull(request.Credentials);
			Assert.Same(System.Net.CredentialCache.DefaultCredentials, request.Credentials);
		}
	}
}
