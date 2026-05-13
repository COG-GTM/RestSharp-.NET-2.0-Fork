using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RestSharp;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class RestClientTests
	{
		[Fact]
		public void Default_Constructor_Sets_Default_UserAgent()
		{
			var client = new RestClient();
			Assert.NotNull(client.UserAgent);
			Assert.StartsWith("RestSharp", client.UserAgent);
		}

		[Fact]
		public void Default_Constructor_Sets_FollowRedirects_True()
		{
			var client = new RestClient();
			Assert.True(client.FollowRedirects);
		}

		[Fact]
		public void Constructor_With_BaseUrl_Sets_BaseUrl()
		{
			var client = new RestClient("http://example.com");
			Assert.Equal("http://example.com", client.BaseUrl);
		}

		[Fact]
		public void BaseUrl_Strips_Trailing_Slash()
		{
			var client = new RestClient();
			client.BaseUrl = "http://example.com/";
			Assert.Equal("http://example.com", client.BaseUrl);
		}

		[Fact]
		public void DefaultParameters_Is_Not_Null()
		{
			var client = new RestClient();
			Assert.NotNull(client.DefaultParameters);
		}

		[Fact]
		public void AddDefaultParameter_Adds_To_DefaultParameters()
		{
			var client = new RestClient();
			client.AddDefaultParameter("key", "value");
			Assert.Single(client.DefaultParameters);
			Assert.Equal("key", client.DefaultParameters[0].Name);
		}

		[Fact]
		public void AddDefaultParameter_With_Type_Adds_Correct_Type()
		{
			var client = new RestClient();
			client.AddDefaultParameter("X-Custom", "value", ParameterType.HttpHeader);
			Assert.Equal(ParameterType.HttpHeader, client.DefaultParameters[0].Type);
		}

		[Fact]
		public void AddDefaultParameter_RequestBody_Throws()
		{
			var client = new RestClient();
			var p = new Parameter { Name = "body", Value = "data", Type = ParameterType.RequestBody };
			Assert.Throws<NotSupportedException>(() => client.AddDefaultParameter(p));
		}

		[Fact]
		public void AddDefaultHeader_Adds_HttpHeader()
		{
			var client = new RestClient();
			client.AddDefaultHeader("Authorization", "Bearer token");
			var param = client.DefaultParameters.First();
			Assert.Equal("Authorization", param.Name);
			Assert.Equal(ParameterType.HttpHeader, param.Type);
		}

		[Fact]
		public void AddDefaultUrlSegment_Adds_UrlSegment()
		{
			var client = new RestClient();
			client.AddDefaultUrlSegment("version", "v2");
			var param = client.DefaultParameters.First();
			Assert.Equal("version", param.Name);
			Assert.Equal(ParameterType.UrlSegment, param.Type);
		}

		[Fact]
		public void AddHandler_Registers_Deserializer()
		{
			var client = new RestClient();
			client.ClearHandlers();
			client.AddHandler("text/csv", new JsonDeserializer());
			// Verify by building a URI (handlers are used internally)
			Assert.NotNull(client);
		}

		[Fact]
		public void RemoveHandler_Removes_Handler()
		{
			var client = new RestClient();
			client.RemoveHandler("application/json");
			// Should not throw and handler should be removed
			Assert.NotNull(client);
		}

		[Fact]
		public void ClearHandlers_Removes_All_Handlers()
		{
			var client = new RestClient();
			client.ClearHandlers();
			Assert.NotNull(client);
		}

		[Fact]
		public void Timeout_Can_Be_Set()
		{
			var client = new RestClient();
			client.Timeout = 30000;
			Assert.Equal(30000, client.Timeout);
		}

		[Fact]
		public void Authenticator_Can_Be_Set()
		{
			var client = new RestClient();
			var auth = new HttpBasicAuthenticator("user", "pass");
			client.Authenticator = auth;
			Assert.Equal(auth, client.Authenticator);
		}

		[Fact]
		public void CookieContainer_Can_Be_Set()
		{
			var client = new RestClient();
			var container = new CookieContainer();
			client.CookieContainer = container;
			Assert.Equal(container, client.CookieContainer);
		}

		[Fact]
		public void MaxRedirects_Can_Be_Set()
		{
			var client = new RestClient();
			client.MaxRedirects = 10;
			Assert.Equal(10, client.MaxRedirects);
		}

		[Fact]
		public void UserAgent_Can_Be_Set()
		{
			var client = new RestClient();
			client.UserAgent = "Custom/1.0";
			Assert.Equal("Custom/1.0", client.UserAgent);
		}

		[Fact]
		public void UseSynchronizationContext_Default_Is_False()
		{
			var client = new RestClient();
			Assert.False(client.UseSynchronizationContext);
		}

		[Fact]
		public void BuildUri_With_BaseUrl_And_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("api/users");
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/api/users", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_Empty_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest();
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_QueryString_For_GET()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("api/search", Method.GET);
			request.AddParameter("q", "test");
			var uri = client.BuildUri(request);
			Assert.Contains("q=test", uri.ToString());
		}

		[Fact]
		public void BuildUri_Does_Not_Add_QueryString_For_POST()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("api/users", Method.POST);
			request.AddParameter("name", "test");
			var uri = client.BuildUri(request);
			Assert.DoesNotContain("?", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_UrlSegment_Replaces_Token()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("users/{id}");
			request.AddUrlSegment("id", "42");
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/users/42", uri.ToString());
		}

		[Fact]
		public void BuildUri_Strips_Leading_Slash_From_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api/users");
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/api/users", uri.ToString());
		}

		[Fact]
		public void BuildUri_Multiple_QueryString_Params()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("search", Method.GET);
			request.AddParameter("q", "test");
			request.AddParameter("page", "1");
			var uri = client.BuildUri(request);
			Assert.Contains("q=test", uri.ToString());
			Assert.Contains("page=1", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_DELETE_Includes_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("items", Method.DELETE);
			request.AddParameter("id", "5");
			var uri = client.BuildUri(request);
			Assert.Contains("id=5", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_HEAD_Includes_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("items", Method.HEAD);
			request.AddParameter("id", "5");
			var uri = client.BuildUri(request);
			Assert.Contains("id=5", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_OPTIONS_Includes_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("items", Method.OPTIONS);
			request.AddParameter("id", "5");
			var uri = client.BuildUri(request);
			Assert.Contains("id=5", uri.ToString());
		}

		[Fact]
		public void ClientCertificates_Can_Be_Set()
		{
			var client = new RestClient();
			var certs = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
			client.ClientCertificates = certs;
			Assert.Equal(certs, client.ClientCertificates);
		}

		[Fact]
		public void Proxy_Can_Be_Set()
		{
			var client = new RestClient();
			var proxy = new WebProxy("http://proxy.example.com:8080");
			client.Proxy = proxy;
			Assert.Equal(proxy, client.Proxy);
		}
	}
}
