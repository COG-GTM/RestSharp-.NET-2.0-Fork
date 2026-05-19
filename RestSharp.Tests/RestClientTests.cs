using System;
using System.Linq;
using System.Net;
using RestSharp.Deserializers;
using RestSharp.Tests.Fakes;
using Xunit;

namespace RestSharp.Tests
{
	public class RestClientTests
	{
		[Fact]
		public void Constructor_Registers_Default_Json_Handlers()
		{
			var client = new RestClient();
			var handler = GetHandler(client, "application/json");
			Assert.NotNull(handler);
			Assert.IsType<JsonDeserializer>(handler);
		}

		[Fact]
		public void Constructor_Registers_Default_Xml_Handlers()
		{
			var client = new RestClient();
			var handler = GetHandler(client, "application/xml");
			Assert.NotNull(handler);
			Assert.IsType<XmlDeserializer>(handler);
		}

		[Fact]
		public void Constructor_Registers_Wildcard_Handler()
		{
			var client = new RestClient();
			var handler = GetHandler(client, "*");
			Assert.NotNull(handler);
		}

		[Fact]
		public void AddHandler_Registers_Custom_Handler()
		{
			var client = new RestClient();
			var customHandler = new JsonDeserializer();
			client.AddHandler("application/custom", customHandler);

			var handler = GetHandler(client, "application/custom");
			Assert.Same(customHandler, handler);
		}

		[Fact]
		public void RemoveHandler_Removes_Handler()
		{
			var client = new RestClient();
			client.RemoveHandler("application/json");

			var handler = GetHandler(client, "application/json");
			// should fall back to wildcard
			Assert.IsType<XmlDeserializer>(handler);
		}

		[Fact]
		public void ClearHandlers_Removes_All_Handlers()
		{
			var client = new RestClient();
			client.ClearHandlers();

			var handler = GetHandler(client, "application/json");
			Assert.Null(handler);
		}

		[Fact]
		public void GetHandler_Handles_Semicolons_In_ContentType()
		{
			var client = new RestClient();
			var handler = GetHandler(client, "application/json; charset=utf-8");
			Assert.NotNull(handler);
			Assert.IsType<JsonDeserializer>(handler);
		}

		[Fact]
		public void GetHandler_Returns_Wildcard_As_Fallback()
		{
			var client = new RestClient();
			var handler = GetHandler(client, "application/unknown");
			Assert.NotNull(handler);
			Assert.IsType<XmlDeserializer>(handler);
		}

		[Fact]
		public void GetHandler_Returns_Null_If_No_Wildcard()
		{
			var client = new RestClient();
			client.ClearHandlers();
			client.AddHandler("application/json", new JsonDeserializer());

			var handler = GetHandler(client, "application/xml");
			Assert.Null(handler);
		}

		[Fact]
		public void AddDefaultParameter_Throws_For_RequestBody()
		{
			var client = new RestClient();
			Assert.Throws<NotSupportedException>(() =>
				client.AddDefaultParameter(new Parameter { Name = "body", Value = "test", Type = ParameterType.RequestBody }));
		}

		[Fact]
		public void AddDefaultParameter_Name_Value_Defaults_To_GetOrPost()
		{
			var client = new RestClient();
			client.AddDefaultParameter("key", "value");
			Assert.Equal(ParameterType.GetOrPost, client.DefaultParameters.First().Type);
		}

		[Fact]
		public void AddDefaultParameter_With_Type_Sets_Correct_Type()
		{
			var client = new RestClient();
			client.AddDefaultParameter("key", "value", ParameterType.HttpHeader);
			Assert.Equal(ParameterType.HttpHeader, client.DefaultParameters.First().Type);
		}

		[Fact]
		public void AddDefaultHeader_Sets_HttpHeader_Type()
		{
			var client = new RestClient();
			client.AddDefaultHeader("X-Custom", "value");
			var param = client.DefaultParameters.First(p => p.Name == "X-Custom");
			Assert.Equal(ParameterType.HttpHeader, param.Type);
		}

		[Fact]
		public void AddDefaultUrlSegment_Sets_UrlSegment_Type()
		{
			var client = new RestClient();
			client.AddDefaultUrlSegment("id", "123");
			var param = client.DefaultParameters.First(p => p.Name == "id");
			Assert.Equal(ParameterType.UrlSegment, param.Type);
		}

		[Fact]
		public void BaseUrl_Strips_Trailing_Slash()
		{
			var client = new RestClient();
			client.BaseUrl = "http://example.com/";
			Assert.Equal("http://example.com", client.BaseUrl);
		}

		[Fact]
		public void BaseUrl_Without_Trailing_Slash_Unchanged()
		{
			var client = new RestClient();
			client.BaseUrl = "http://example.com";
			Assert.Equal("http://example.com", client.BaseUrl);
		}

		[Fact]
		public void BuildUri_With_Empty_Resource()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest();
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com", uri.ToString());
		}

		[Fact]
		public void BuildUri_BaseUrl_Only()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("");
			var uri = client.BuildUri(request);
			Assert.Equal("http://example.com", uri.ToString());
		}

		[Fact]
		public void BuildUri_Appends_QueryString_For_GET()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.GET);
			request.AddParameter("key", "value");
			var uri = client.BuildUri(request);
			Assert.Contains("key=value", uri.ToString());
		}

		[Fact]
		public void BuildUri_Does_Not_Append_QueryString_For_POST()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("resource", Method.POST);
			request.AddParameter("key", "value");
			var uri = client.BuildUri(request);
			Assert.DoesNotContain("key=value", uri.ToString());
		}

		[Fact]
		public void ConfigureHttp_Maps_Parameters_Headers_Cookies_Files_Body()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.UserAgent = "TestAgent";
			client.Timeout = 5000;

			var request = new RestRequest("resource", Method.POST);
			request.AddParameter("key", "value");
			request.AddHeader("X-Custom", "headerval");
			request.AddCookie("session", "abc");
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.bin");
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "Test" });

			client.Execute(request);

			Assert.True(fakeHttp.Parameters.Any(p => p.Name == "key" && p.Value == "value"));
			Assert.True(fakeHttp.Headers.Any(h => h.Name == "X-Custom" && h.Value == "headerval"));
			Assert.True(fakeHttp.Cookies.Any(c => c.Name == "session" && c.Value == "abc"));
			Assert.Equal(1, fakeHttp.Files.Count);
			Assert.NotNull(fakeHttp.RequestBody);
			Assert.Equal("TestAgent", fakeHttp.UserAgent);
		}

		[Fact]
		public void ConfigureHttp_Maps_Timeout_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.Timeout = 5000;

			var request = new RestRequest("resource");
			request.Timeout = 1000;

			client.Execute(request);

			Assert.Equal(1000, fakeHttp.Timeout);
		}

		[Fact]
		public void ConfigureHttp_Uses_Client_Timeout_When_Request_Is_Zero()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.Timeout = 5000;

			var request = new RestRequest("resource");

			client.Execute(request);

			Assert.Equal(5000, fakeHttp.Timeout);
		}

		[Fact]
		public void ConfigureHttp_Maps_Credentials()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;

			var request = new RestRequest("resource");
			request.Credentials = new NetworkCredential("user", "pass");

			client.Execute(request);

			Assert.NotNull(fakeHttp.Credentials);
		}

		[Fact]
		public void ConvertToRestResponse_Maps_All_Fields()
		{
			var fakeHttp = new FakeHttp();
			fakeHttp.CannedResponse = new HttpResponse
			{
				ContentType = "application/json",
				ContentEncoding = "utf-8",
				ContentLength = 100,
				StatusCode = HttpStatusCode.OK,
				StatusDescription = "OK",
				Server = "TestServer",
				ResponseUri = new Uri("http://example.com"),
				RawBytes = System.Text.Encoding.UTF8.GetBytes("test"),
				ResponseStatus = ResponseStatus.Completed
			};
			fakeHttp.CannedResponse.Headers.Add(new HttpHeader { Name = "X-Test", Value = "val" });

			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;

			var response = client.Execute(new RestRequest("resource"));

			Assert.Equal("application/json", response.ContentType);
			Assert.Equal("utf-8", response.ContentEncoding);
			Assert.Equal(100, response.ContentLength);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Equal("OK", response.StatusDescription);
			Assert.Equal("TestServer", response.Server);
			Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
			Assert.True(response.Headers.Any(h => h.Name == "X-Test"));
		}

		[Fact]
		public void AuthenticateIfNeeded_Calls_Authenticator_When_Set()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.Authenticator = new HttpBasicAuthenticator("user", "pass");

			var request = new RestRequest("resource");
			client.Execute(request);

			Assert.True(request.Parameters.Any(p => p.Name == "Authorization"));
		}

		[Fact]
		public void AuthenticateIfNeeded_Does_Nothing_When_Null()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.Authenticator = null;

			var request = new RestRequest("resource");
			client.Execute(request);

			Assert.False(request.Parameters.Any(p => p.Name == "Authorization"));
		}

		[Fact]
		public void FollowRedirects_Defaults_To_True()
		{
			var client = new RestClient();
			Assert.True(client.FollowRedirects);
		}

		[Fact]
		public void DefaultParameters_Merge_With_Request_Parameters()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.AddDefaultParameter("default_key", "default_value");

			var request = new RestRequest("resource");
			client.Execute(request);

			Assert.True(fakeHttp.Parameters.Any(p => p.Name == "default_key"));
		}

		[Fact]
		public void Request_Parameter_Overrides_Default_Parameter()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.AddDefaultParameter("key", "default");

			var request = new RestRequest("resource");
			request.AddParameter("key", "override");
			client.Execute(request);

			var param = fakeHttp.Parameters.First(p => p.Name == "key");
			Assert.Equal("override", param.Value);
		}

		private IDeserializer GetHandler(RestClient client, string contentType)
		{
			// Use reflection to access the private GetHandler method
			var method = typeof(RestClient).GetMethod("GetHandler",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return (IDeserializer)method.Invoke(client, new object[] { contentType });
		}
	}
}
