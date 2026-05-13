using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	/// <summary>
	/// Tests that exercise RestClient's internal methods (ConfigureHttp, ConvertToRestResponse, etc.)
	/// through the Execute method with a fake HTTP implementation.
	/// </summary>
	public class RestClientIntegrationTests
	{
		[Fact]
		public void Execute_With_FakeHttp_Returns_Response()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"name\":\"test\"}"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				StatusDescription = "OK",
				ContentEncoding = "utf-8",
				ContentLength = 15,
				ResponseUri = new Uri("http://example.com/api"),
				Server = "TestServer",
				ResponseStatus = ResponseStatus.Completed
			});

			var request = new RestRequest("api", Method.GET);
			var response = client.Execute(request);

			Assert.Equal("{\"name\":\"test\"}", response.Content);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Equal("OK", response.StatusDescription);
			Assert.Equal("TestServer", response.Server);
			Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
		}

		[Fact]
		public void Execute_With_POST_Uses_PostMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.AddParameter("data", "value");
			var response = client.Execute(request);

			Assert.Equal("POST", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_With_PUT_Uses_PutMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.PUT);
			client.Execute(request);

			Assert.Equal("PUT", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_With_DELETE_Uses_DeleteMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.DELETE);
			client.Execute(request);

			Assert.Equal("DELETE", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_With_HEAD_Uses_HeadMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.HEAD);
			client.Execute(request);

			Assert.Equal("HEAD", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_With_OPTIONS_Uses_OptionsMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.OPTIONS);
			client.Execute(request);

			Assert.Equal("OPTIONS", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_With_PATCH_Uses_PatchMethod()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.PATCH);
			client.Execute(request);

			Assert.Equal("PATCH", fakeHttp.LastMethod);
		}

		[Fact]
		public void Execute_Configures_Headers_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.AddHeader("X-Custom", "CustomValue");
			client.Execute(request);

			Assert.Contains(fakeHttp.Headers, h => h.Name == "X-Custom" && h.Value == "CustomValue");
		}

		[Fact]
		public void Execute_Configures_Cookies_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.AddCookie("session", "abc");
			client.Execute(request);

			Assert.Contains(fakeHttp.Cookies, c => c.Name == "session" && c.Value == "abc");
		}

		[Fact]
		public void Execute_Configures_Parameters_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.AddParameter("key", "val");
			client.Execute(request);

			Assert.Contains(fakeHttp.Parameters, p => p.Name == "key" && p.Value == "val");
		}

		[Fact]
		public void Execute_Configures_RequestBody_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "test" });
			client.Execute(request);

			Assert.NotNull(fakeHttp.RequestBody);
			Assert.Contains("test", fakeHttp.RequestBody);
		}

		[Fact]
		public void Execute_Sets_UserAgent()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.UserAgent = "TestAgent/1.0";
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Equal("TestAgent/1.0", fakeHttp.UserAgent);
		}

		[Fact]
		public void Execute_Sets_Timeout_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.Timeout = 5000;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.Timeout = 3000;
			client.Execute(request);

			Assert.Equal(3000, fakeHttp.Timeout);
		}

		[Fact]
		public void Execute_Sets_Timeout_From_Client_When_Request_Zero()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.Timeout = 5000;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Equal(5000, fakeHttp.Timeout);
		}

		[Fact]
		public void Execute_Merges_DefaultParameters()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.AddDefaultHeader("X-Default", "default-value");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Contains(fakeHttp.Headers, h => h.Name == "X-Default" && h.Value == "default-value");
		}

		[Fact]
		public void Execute_Request_Overrides_DefaultParameters()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.AddDefaultHeader("X-Custom", "default");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.AddHeader("X-Custom", "override");
			client.Execute(request);

			var customHeaders = fakeHttp.Headers.Where(h => h.Name == "X-Custom").ToList();
			Assert.Single(customHeaders);
			Assert.Equal("override", customHeaders[0].Value);
		}

		[Fact]
		public void Execute_With_Authenticator_Authenticates_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.Authenticator = new HttpBasicAuthenticator("user", "pass");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Contains(fakeHttp.Headers, h => h.Name == "Authorization");
		}

		[Fact]
		public void Execute_Increments_Request_Attempts()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			Assert.Equal(0, request.Attempts);
			client.Execute(request);
			Assert.Equal(1, request.Attempts);
		}

		[Fact]
		public void Execute_Handles_Exception_Sets_Error_Status()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new ThrowingHttpFactory();

			var request = new RestRequest("api", Method.GET);
			var response = client.Execute(request);

			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
			Assert.NotNull(response.ErrorException);
		}

		[Fact]
		public void Execute_Generic_Returns_Deserialized_Data()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"Name\":\"test\",\"Value\":42}"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			var request = new RestRequest("api", Method.GET);
			var response = client.Execute<SimpleData>(request);

			Assert.NotNull(response.Data);
			Assert.Equal("test", response.Data.Name);
			Assert.Equal(42, response.Data.Value);
		}

		[Fact]
		public void ConvertToRestResponse_Copies_Headers()
		{
			var client = new RestClient("http://example.com");
			var fakeResponse = new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("body"),
				ContentType = "text/plain",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			};
			fakeResponse.Headers.Add(new HttpHeader { Name = "X-Test", Value = "123" });

			client.HttpFactory = new FakeHttpFactory(fakeResponse);
			var request = new RestRequest("api", Method.GET);
			var response = client.Execute(request);

			Assert.Contains(response.Headers, h => h.Name == "X-Test" && h.Value.ToString() == "123");
		}

		[Fact]
		public void ConvertToRestResponse_Copies_Cookies()
		{
			var client = new RestClient("http://example.com");
			var fakeResponse = new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("body"),
				ContentType = "text/plain",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			};
			fakeResponse.Cookies.Add(new HttpCookie
			{
				Name = "session",
				Value = "abc",
				Domain = "example.com",
				Path = "/",
				HttpOnly = true,
				Secure = true
			});

			client.HttpFactory = new FakeHttpFactory(fakeResponse);
			var request = new RestRequest("api", Method.GET);
			var response = client.Execute(request);

			Assert.Single(response.Cookies);
			Assert.Equal("session", response.Cookies[0].Name);
			Assert.Equal("abc", response.Cookies[0].Value);
			Assert.Equal("example.com", response.Cookies[0].Domain);
			Assert.True(response.Cookies[0].HttpOnly);
			Assert.True(response.Cookies[0].Secure);
		}

		[Fact]
		public void Execute_Sets_FollowRedirects()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.FollowRedirects = false;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.False(fakeHttp.FollowRedirects);
		}

		[Fact]
		public void Execute_Sets_MaxRedirects()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.MaxRedirects = 3;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Equal(3, fakeHttp.MaxRedirects);
		}

		[Fact]
		public void Execute_Sets_CookieContainer()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			var container = new CookieContainer();
			client.CookieContainer = container;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Equal(container, fakeHttp.CookieContainer);
		}

		[Fact]
		public void Execute_Sets_Proxy()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			var proxy = new WebProxy("http://proxy.example.com");
			client.Proxy = proxy;
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Equal(proxy, fakeHttp.Proxy);
		}

		[Fact]
		public void Execute_Sets_Credentials_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			var creds = new NetworkCredential("user", "pass");
			request.Credentials = creds;
			client.Execute(request);

			Assert.Equal(creds, fakeHttp.Credentials);
		}

		[Fact]
		public void Execute_Configures_Files_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.bin");
			client.Execute(request);

			Assert.Single(fakeHttp.Files);
			Assert.Equal("file", fakeHttp.Files[0].Name);
		}

		[Fact]
		public void DownloadData_Returns_RawBytes()
		{
			var rawData = new byte[] { 10, 20, 30 };
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				ContentType = "application/octet-stream",
				RawBytes = rawData,
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			var request = new RestRequest("data", Method.GET);
			var result = client.DownloadData(request);

			Assert.Equal(rawData, result);
		}

		public class SimpleData
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
	}

	#region Fake HTTP implementations

	public class FakeHttp : IHttp
	{
		public string LastMethod { get; private set; }
		public CookieContainer CookieContainer { get; set; }
		public ICredentials Credentials { get; set; }
		public string UserAgent { get; set; }
		public int Timeout { get; set; }
		public bool FollowRedirects { get; set; }
		public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get; set; }
		public int? MaxRedirects { get; set; }
		public IList<HttpHeader> Headers { get; private set; }
		public IList<HttpParameter> Parameters { get; private set; }
		public IList<HttpFile> Files { get; private set; }
		public IList<HttpCookie> Cookies { get; private set; }
		public string RequestBody { get; set; }
		public string RequestContentType { get; set; }
		public Uri Url { get; set; }
		public IWebProxy Proxy { get; set; }

		public FakeHttp()
		{
			Headers = new List<HttpHeader>();
			Parameters = new List<HttpParameter>();
			Files = new List<HttpFile>();
			Cookies = new List<HttpCookie>();
		}

		private HttpResponse MakeResponse(string method)
		{
			LastMethod = method;
			return new HttpResponse { ResponseStatus = ResponseStatus.Completed, StatusCode = HttpStatusCode.OK, RawBytes = new byte[0] };
		}

		public HttpResponse Delete() => MakeResponse("DELETE");
		public HttpResponse Get() => MakeResponse("GET");
		public HttpResponse Head() => MakeResponse("HEAD");
		public HttpResponse Options() => MakeResponse("OPTIONS");
		public HttpResponse Post() => MakeResponse("POST");
		public HttpResponse Put() => MakeResponse("PUT");
		public HttpResponse Patch() => MakeResponse("PATCH");

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action) { action(MakeResponse("DELETE")); return null; }
		public HttpWebRequest GetAsync(Action<HttpResponse> action) { action(MakeResponse("GET")); return null; }
		public HttpWebRequest HeadAsync(Action<HttpResponse> action) { action(MakeResponse("HEAD")); return null; }
		public HttpWebRequest OptionsAsync(Action<HttpResponse> action) { action(MakeResponse("OPTIONS")); return null; }
		public HttpWebRequest PostAsync(Action<HttpResponse> action) { action(MakeResponse("POST")); return null; }
		public HttpWebRequest PutAsync(Action<HttpResponse> action) { action(MakeResponse("PUT")); return null; }
		public HttpWebRequest PatchAsync(Action<HttpResponse> action) { action(MakeResponse("PATCH")); return null; }
	}

	public class FakeHttpFactory : IHttpFactory
	{
		private readonly HttpResponse _response;

		public FakeHttpFactory(HttpResponse response)
		{
			_response = response;
		}

		public IHttp Create()
		{
			return new FakeHttpWithResponse(_response);
		}
	}

	public class FakeHttpWithResponse : IHttp
	{
		private readonly HttpResponse _response;

		public FakeHttpWithResponse(HttpResponse response)
		{
			_response = response;
			Headers = new List<HttpHeader>();
			Parameters = new List<HttpParameter>();
			Files = new List<HttpFile>();
			Cookies = new List<HttpCookie>();
		}

		public CookieContainer CookieContainer { get; set; }
		public ICredentials Credentials { get; set; }
		public string UserAgent { get; set; }
		public int Timeout { get; set; }
		public bool FollowRedirects { get; set; }
		public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get; set; }
		public int? MaxRedirects { get; set; }
		public IList<HttpHeader> Headers { get; private set; }
		public IList<HttpParameter> Parameters { get; private set; }
		public IList<HttpFile> Files { get; private set; }
		public IList<HttpCookie> Cookies { get; private set; }
		public string RequestBody { get; set; }
		public string RequestContentType { get; set; }
		public Uri Url { get; set; }
		public IWebProxy Proxy { get; set; }

		public HttpResponse Delete() => _response;
		public HttpResponse Get() => _response;
		public HttpResponse Head() => _response;
		public HttpResponse Options() => _response;
		public HttpResponse Post() => _response;
		public HttpResponse Put() => _response;
		public HttpResponse Patch() => _response;

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest GetAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest HeadAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest OptionsAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest PostAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest PutAsync(Action<HttpResponse> action) { action(_response); return null; }
		public HttpWebRequest PatchAsync(Action<HttpResponse> action) { action(_response); return null; }
	}

	public class FakeHttpFactoryInstance : IHttpFactory
	{
		private readonly IHttp _instance;

		public FakeHttpFactoryInstance(IHttp instance)
		{
			_instance = instance;
		}

		public IHttp Create()
		{
			return _instance;
		}
	}

	public class ThrowingHttpFactory : IHttpFactory
	{
		public IHttp Create()
		{
			return new ThrowingHttp();
		}
	}

	public class ThrowingHttp : IHttp
	{
		public CookieContainer CookieContainer { get; set; }
		public ICredentials Credentials { get; set; }
		public string UserAgent { get; set; }
		public int Timeout { get; set; }
		public bool FollowRedirects { get; set; }
		public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get; set; }
		public int? MaxRedirects { get; set; }
		public IList<HttpHeader> Headers { get; private set; }
		public IList<HttpParameter> Parameters { get; private set; }
		public IList<HttpFile> Files { get; private set; }
		public IList<HttpCookie> Cookies { get; private set; }
		public string RequestBody { get; set; }
		public string RequestContentType { get; set; }
		public Uri Url { get; set; }
		public IWebProxy Proxy { get; set; }

		public ThrowingHttp()
		{
			Headers = new List<HttpHeader>();
			Parameters = new List<HttpParameter>();
			Files = new List<HttpFile>();
			Cookies = new List<HttpCookie>();
		}

		public HttpResponse Delete() => throw new Exception("Test exception");
		public HttpResponse Get() => throw new Exception("Test exception");
		public HttpResponse Head() => throw new Exception("Test exception");
		public HttpResponse Options() => throw new Exception("Test exception");
		public HttpResponse Post() => throw new Exception("Test exception");
		public HttpResponse Put() => throw new Exception("Test exception");
		public HttpResponse Patch() => throw new Exception("Test exception");

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest GetAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest HeadAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest OptionsAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest PostAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest PutAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
		public HttpWebRequest PatchAsync(Action<HttpResponse> action) => throw new Exception("Test exception");
	}

	#endregion
}
