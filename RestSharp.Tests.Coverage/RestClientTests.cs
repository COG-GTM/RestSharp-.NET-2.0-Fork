using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests
{
	public class RestClientTests
	{
		[Fact]
		public void Default_Constructor_Registers_Default_Handlers()
		{
			var client = new RestClient();
			Assert.NotNull(client.DefaultParameters);
			Assert.NotNull(client.UserAgent);
			Assert.Contains("RestSharp", client.UserAgent);
			Assert.True(client.FollowRedirects);
		}

		[Fact]
		public void Constructor_With_BaseUrl()
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
		public void BaseUrl_Preserves_Path_Without_Trailing_Slash()
		{
			var client = new RestClient();
			client.BaseUrl = "http://example.com/api";
			Assert.Equal("http://example.com/api", client.BaseUrl);
		}

		[Fact]
		public void AddDefaultParameter_Adds_Parameter()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultParameter("key", "value");
			Assert.Single(client.DefaultParameters);
			Assert.Equal("key", client.DefaultParameters[0].Name);
		}

		[Fact]
		public void AddDefaultParameter_With_Type()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultParameter("key", "value", ParameterType.HttpHeader);
			Assert.Equal(ParameterType.HttpHeader, client.DefaultParameters[0].Type);
		}

		[Fact]
		public void AddDefaultParameter_RequestBody_Throws()
		{
			var client = new RestClient("http://example.com");
			var p = new Parameter { Name = "body", Value = "data", Type = ParameterType.RequestBody };
			Assert.Throws<NotSupportedException>(() => client.AddDefaultParameter(p));
		}

		[Fact]
		public void AddDefaultHeader_Adds_HttpHeader_Parameter()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultHeader("X-Api-Key", "12345");
			Assert.Single(client.DefaultParameters);
			Assert.Equal(ParameterType.HttpHeader, client.DefaultParameters[0].Type);
			Assert.Equal("X-Api-Key", client.DefaultParameters[0].Name);
		}

		[Fact]
		public void AddDefaultUrlSegment_Adds_UrlSegment_Parameter()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultUrlSegment("version", "v2");
			Assert.Single(client.DefaultParameters);
			Assert.Equal(ParameterType.UrlSegment, client.DefaultParameters[0].Type);
			Assert.Equal("version", client.DefaultParameters[0].Name);
		}

		[Fact]
		public void AddHandler_And_RemoveHandler()
		{
			var client = new RestClient();
			var handler = new JsonDeserializer();
			client.AddHandler("application/custom", handler);
			client.RemoveHandler("application/custom");
			// no exception means success
		}

		[Fact]
		public void ClearHandlers_Removes_All()
		{
			var client = new RestClient();
			client.ClearHandlers();
			// after clearing, adding a new handler should work fine
			client.AddHandler("text/plain", new JsonDeserializer());
		}

		[Fact]
		public void BuildUri_With_QueryString_Parameters()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource");
			request.AddParameter("foo", "bar");
			request.AddParameter("baz", "qux");

			var uri = client.BuildUri(request);
			Assert.Contains("foo=bar", uri.ToString());
			Assert.Contains("baz=qux", uri.ToString());
		}

		[Fact]
		public void BuildUri_POST_Does_Not_Add_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.POST);
			request.AddParameter("foo", "bar");

			var uri = client.BuildUri(request);
			Assert.DoesNotContain("?", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_Multiple_UrlSegments()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("api/{version}/users/{id}");
			request.AddUrlSegment("version", "v2");
			request.AddUrlSegment("id", "42");

			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/api/v2/users/42", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_Empty_BaseUrl_And_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest();

			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/", uri.ToString());
		}

		[Fact]
		public void BuildUri_Strips_Trailing_Slash_On_Resource_Before_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource/");
			request.AddParameter("key", "val");

			var uri = client.BuildUri(request);
			Assert.Contains("resource?key=val", uri.ToString());
		}

		[Fact]
		public void Timeout_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			client.Timeout = 30000;
			Assert.Equal(30000, client.Timeout);
		}

		[Fact]
		public void UserAgent_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			client.UserAgent = "CustomAgent/1.0";
			Assert.Equal("CustomAgent/1.0", client.UserAgent);
		}

		[Fact]
		public void CookieContainer_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			var container = new CookieContainer();
			client.CookieContainer = container;
			Assert.Same(container, client.CookieContainer);
		}

		[Fact]
		public void MaxRedirects_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			client.MaxRedirects = 5;
			Assert.Equal(5, client.MaxRedirects);
		}

		[Fact]
		public void Authenticator_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			var auth = new HttpBasicAuthenticator("user", "pass");
			client.Authenticator = auth;
			Assert.Same(auth, client.Authenticator);
		}

		[Fact]
		public void FollowRedirects_Defaults_True()
		{
			var client = new RestClient();
			Assert.True(client.FollowRedirects);
		}

		[Fact]
		public void FollowRedirects_Can_Be_Set_False()
		{
			var client = new RestClient();
			client.FollowRedirects = false;
			Assert.False(client.FollowRedirects);
		}

		[Fact]
		public void BuildUri_PUT_Does_Not_Add_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.PUT);
			request.AddParameter("foo", "bar");

			var uri = client.BuildUri(request);
			Assert.DoesNotContain("?", uri.ToString());
		}

		[Fact]
		public void BuildUri_PATCH_Does_Not_Add_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.PATCH);
			request.AddParameter("foo", "bar");

			var uri = client.BuildUri(request);
			Assert.DoesNotContain("?", uri.ToString());
		}

		[Fact]
		public void BuildUri_DELETE_Adds_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.DELETE);
			request.AddParameter("id", "42");

			var uri = client.BuildUri(request);
			Assert.Contains("id=42", uri.ToString());
		}

		[Fact]
		public void BuildUri_HEAD_Adds_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.HEAD);
			request.AddParameter("check", "true");

			var uri = client.BuildUri(request);
			Assert.Contains("check=true", uri.ToString());
		}

		[Fact]
		public void BuildUri_OPTIONS_Adds_QueryString()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.OPTIONS);
			request.AddParameter("verbose", "1");

			var uri = client.BuildUri(request);
			Assert.Contains("verbose=1", uri.ToString());
		}

		[Fact]
		public void Proxy_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			var proxy = new WebProxy("http://proxy.local:8080");
			client.Proxy = proxy;
			Assert.Same(proxy, client.Proxy);
		}

		[Fact]
		public void ClientCertificates_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			var certs = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
			client.ClientCertificates = certs;
			Assert.Same(certs, client.ClientCertificates);
		}

		[Fact]
		public void HttpFactory_Is_Set_By_Default()
		{
			var client = new RestClient();
			Assert.NotNull(client.HttpFactory);
		}
	}
}
