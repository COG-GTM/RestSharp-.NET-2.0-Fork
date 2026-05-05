using System;
using System.Linq;
using System.Net;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests
{
	public class RestClientAdditionalTests
	{
		[Fact]
		public void AddDefaultParameter_GetOrPost()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultParameter("key", "value");
			Assert.Contains(client.DefaultParameters, p => p.Name == "key" && p.Type == ParameterType.GetOrPost);
		}

		[Fact]
		public void AddDefaultParameter_WithType()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultParameter("key", "value", ParameterType.HttpHeader);
			Assert.Contains(client.DefaultParameters, p => p.Name == "key" && p.Type == ParameterType.HttpHeader);
		}

		[Fact]
		public void AddDefaultParameter_RequestBody_Throws()
		{
			var client = new RestClient("http://example.com");
			Assert.Throws<NotSupportedException>(() =>
				client.AddDefaultParameter(new Parameter { Name = "body", Value = "data", Type = ParameterType.RequestBody }));
		}

		[Fact]
		public void AddDefaultHeader()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultHeader("X-Api-Key", "12345");
			Assert.Contains(client.DefaultParameters, p => p.Name == "X-Api-Key" && p.Type == ParameterType.HttpHeader);
		}

		[Fact]
		public void AddDefaultUrlSegment()
		{
			var client = new RestClient("http://example.com");
			client.AddDefaultUrlSegment("version", "v2");
			Assert.Contains(client.DefaultParameters, p => p.Name == "version" && p.Type == ParameterType.UrlSegment);
		}

		[Fact]
		public void AddHandler_And_RemoveHandler()
		{
			var client = new RestClient("http://example.com");
			client.AddHandler("text/csv", new JsonDeserializer());
			client.RemoveHandler("text/csv");
		}

		[Fact]
		public void ClearHandlers()
		{
			var client = new RestClient("http://example.com");
			client.ClearHandlers();
		}

		[Fact]
		public void BaseUrl_Strips_Trailing_Slash()
		{
			var client = new RestClient();
			client.BaseUrl = "http://example.com/";
			Assert.Equal("http://example.com", client.BaseUrl);
		}

		[Fact]
		public void BuildUri_With_UrlSegment()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/users/{id}");
			request.AddUrlSegment("id", "42");
			var uri = client.BuildUri(request);
			Assert.Contains("42", uri.ToString());
		}

		[Fact]
		public void BuildUri_With_GetOrPost_Params_On_GET()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/search", Method.GET);
			request.AddParameter("q", "test");
			var uri = client.BuildUri(request);
			Assert.Contains("?q=test", uri.ToString());
		}

		[Fact]
		public void BuildUri_No_QueryString_On_POST()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api", Method.POST);
			request.AddParameter("key", "val");
			var uri = client.BuildUri(request);
			Assert.DoesNotContain("?", uri.ToString());
		}

		[Fact]
		public void BuildUri_Empty_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest();
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com/", uri.ToString());
		}

		[Fact]
		public void BuildUri_Resource_With_Leading_Slash()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api/test");
			var uri = client.BuildUri(request);
			Assert.Contains("api/test", uri.ToString());
		}

		[Fact]
		public void Properties()
		{
			var client = new RestClient();
			client.MaxRedirects = 5;
			client.FollowRedirects = false;
			client.CookieContainer = new CookieContainer();
			client.UserAgent = "Custom/1.0";
			client.Timeout = 5000;
			client.UseSynchronizationContext = true;

			Assert.Equal(5, client.MaxRedirects);
			Assert.False(client.FollowRedirects);
			Assert.NotNull(client.CookieContainer);
			Assert.Equal("Custom/1.0", client.UserAgent);
			Assert.Equal(5000, client.Timeout);
			Assert.True(client.UseSynchronizationContext);
		}

		[Fact]
		public void Execute_Invalid_BaseUrl_Returns_Error()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_Generic_Invalid_BaseUrl_Returns_Error()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test");
			var response = client.Execute<TestData>(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_Default_Headers()
		{
			var client = new RestClient("http://localhost:1");
			client.AddDefaultHeader("X-Test", "value");
			var request = new RestRequest("/test");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_Authenticator()
		{
			var client = new RestClient("http://localhost:1");
			client.Authenticator = new HttpBasicAuthenticator("user", "pass");
			var request = new RestRequest("/test");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_CookieContainer()
		{
			var client = new RestClient("http://localhost:1");
			client.CookieContainer = new CookieContainer();
			var request = new RestRequest("/test");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_POST()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.POST);
			request.AddParameter("key", "val");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_PUT()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.PUT);
			request.AddParameter("key", "val");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_DELETE()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.DELETE);
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_HEAD()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.HEAD);
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_OPTIONS()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.OPTIONS);
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_PATCH()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test", Method.PATCH);
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_Cookie_Parameter()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test");
			request.AddCookie("session", "abc");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_Custom_Timeout()
		{
			var client = new RestClient("http://localhost:1");
			client.Timeout = 500;
			var request = new RestRequest("/test");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_Default_Parameters_No_Override()
		{
			var client = new RestClient("http://localhost:1");
			client.AddDefaultParameter("defaultKey", "defaultVal");
			var request = new RestRequest("/test");
			request.AddParameter("requestKey", "requestVal");
			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void FollowRedirects_Default_True()
		{
			var client = new RestClient();
			Assert.True(client.FollowRedirects);
		}

		[Fact]
		public void UserAgent_Contains_RestSharp()
		{
			var client = new RestClient();
			Assert.Contains("RestSharp", client.UserAgent);
		}

		public class TestData
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
	}
}
