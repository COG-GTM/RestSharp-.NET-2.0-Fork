using System;
using System.Linq;
using System.Text;
using RestSharp;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class HttpBasicAuthenticatorTests
	{
		[Fact]
		public void Authenticate_Adds_Authorization_Header()
		{
			var auth = new HttpBasicAuthenticator("user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.NotNull(header);
			Assert.Equal(ParameterType.HttpHeader, header.Type);

			var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));
			Assert.Equal(string.Format("Basic {0}", expected), header.Value);
		}

		[Fact]
		public void Authenticate_Does_Not_Duplicate_Authorization_Header()
		{
			var auth = new HttpBasicAuthenticator("user", "pass");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			request.AddHeader("Authorization", "Existing");
			auth.Authenticate(client, request);

			var headers = request.Parameters.Where(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToList();
			Assert.Single(headers);
			Assert.Equal("Existing", headers[0].Value);
		}

		[Fact]
		public void Authenticate_With_Empty_Username_And_Password()
		{
			var auth = new HttpBasicAuthenticator("", "");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.NotNull(header);
			var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(":"));
			Assert.Equal(string.Format("Basic {0}", expected), header.Value);
		}

		[Fact]
		public void Authenticate_With_Special_Characters()
		{
			var auth = new HttpBasicAuthenticator("user@domain.com", "p@ss:word!");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.NotNull(header);
			var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("user@domain.com:p@ss:word!"));
			Assert.Equal(string.Format("Basic {0}", expected), header.Value);
		}
	}

	public class SimpleAuthenticatorTests
	{
		[Fact]
		public void Authenticate_Adds_Username_And_Password_Parameters()
		{
			var auth = new SimpleAuthenticator("username", "admin", "password", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var userParam = request.Parameters.FirstOrDefault(p => p.Name == "username");
			var passParam = request.Parameters.FirstOrDefault(p => p.Name == "password");

			Assert.NotNull(userParam);
			Assert.Equal("admin", userParam.Value);
			Assert.NotNull(passParam);
			Assert.Equal("secret", passParam.Value);
		}

		[Fact]
		public void Authenticate_Uses_GetOrPost_ParameterType()
		{
			var auth = new SimpleAuthenticator("user", "admin", "pass", "secret");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			foreach (var param in request.Parameters)
			{
				Assert.Equal(ParameterType.GetOrPost, param.Type);
			}
		}
	}

	public class OAuth2AuthenticatorTests
	{
		[Fact]
		public void UriQueryParameter_Adds_Token_As_GetOrPost_Parameter()
		{
			var auth = new OAuth2UriQueryParameterAuthenticator("my_token_123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var param = request.Parameters.FirstOrDefault(p => p.Name == "oauth_token");
			Assert.NotNull(param);
			Assert.Equal("my_token_123", param.Value);
			Assert.Equal(ParameterType.GetOrPost, param.Type);
		}

		[Fact]
		public void AuthorizationRequestHeader_Adds_Bearer_Header()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("my_token_123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			var header = request.Parameters.FirstOrDefault(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
			Assert.NotNull(header);
			Assert.Equal("OAuth my_token_123", header.Value);
			Assert.Equal(ParameterType.HttpHeader, header.Type);
		}

		[Fact]
		public void AuthorizationRequestHeader_Does_Not_Duplicate_Header()
		{
			var auth = new OAuth2AuthorizationRequestHeaderAuthenticator("my_token_123");
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			request.AddHeader("Authorization", "Existing");
			auth.Authenticate(client, request);

			var headers = request.Parameters.Where(p =>
				p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToList();
			Assert.Single(headers);
			Assert.Equal("Existing", headers[0].Value);
		}

		[Fact]
		public void UriQueryParameter_AccessToken_Property()
		{
			var auth = new OAuth2UriQueryParameterAuthenticator("token_abc");
			Assert.Equal("token_abc", auth.AccessToken);
		}
	}

	public class NtlmAuthenticatorTests
	{
		[Fact]
		public void Authenticate_Sets_Default_Credentials()
		{
			var auth = new NtlmAuthenticator();
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			auth.Authenticate(client, request);

			Assert.Equal(System.Net.CredentialCache.DefaultCredentials, request.Credentials);
		}
	}
}
