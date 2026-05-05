using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace RestSharp.Tests
{
	public class HttpSyncTests
	{
		[Fact]
		public void Create_Returns_New_Http_Instance()
		{
			var http = new Http();
			var created = http.Create();
			Assert.NotNull(created);
			Assert.IsType<Http>(created);
		}

		[Fact]
		public void Default_Constructor_Initializes_Collections()
		{
			var http = new Http();
			Assert.NotNull(http.Headers);
			Assert.NotNull(http.Files);
			Assert.NotNull(http.Parameters);
			Assert.NotNull(http.Cookies);
			Assert.Empty(http.Headers);
			Assert.Empty(http.Files);
			Assert.Empty(http.Parameters);
			Assert.Empty(http.Cookies);
		}

		[Fact]
		public void All_Properties_Can_Be_Set()
		{
			var http = new Http();
			http.UserAgent = "TestAgent/1.0";
			http.Timeout = 30000;
			http.Credentials = new NetworkCredential("user", "pass");
			http.CookieContainer = new CookieContainer();
			http.FollowRedirects = true;
			http.ClientCertificates = new X509CertificateCollection();
			http.MaxRedirects = 5;
			http.RequestBody = "{\"test\":true}";
			http.RequestContentType = "application/json";
			http.Url = new Uri("http://example.com");
			http.Proxy = new WebProxy("http://proxy.com");

			Assert.Equal("TestAgent/1.0", http.UserAgent);
			Assert.Equal(30000, http.Timeout);
			Assert.NotNull(http.Credentials);
			Assert.NotNull(http.CookieContainer);
			Assert.True(http.FollowRedirects);
			Assert.NotNull(http.ClientCertificates);
			Assert.Equal(5, http.MaxRedirects);
			Assert.Equal("{\"test\":true}", http.RequestBody);
			Assert.Equal("application/json", http.RequestContentType);
			Assert.Equal(new Uri("http://example.com"), http.Url);
			Assert.NotNull(http.Proxy);
		}

		[Fact]
		public void Headers_Can_Be_Added()
		{
			var http = new Http();
			http.Headers.Add(new HttpHeader { Name = "X-Custom", Value = "test" });
			Assert.Single(http.Headers);
		}

		[Fact]
		public void Parameters_Can_Be_Added()
		{
			var http = new Http();
			http.Parameters.Add(new HttpParameter { Name = "param1", Value = "value1" });
			Assert.Single(http.Parameters);
		}

		[Fact]
		public void Cookies_Can_Be_Added()
		{
			var http = new Http();
			http.Cookies.Add(new HttpCookie { Name = "session", Value = "abc" });
			Assert.Single(http.Cookies);
		}

		[Fact]
		public void Files_Can_Be_Added()
		{
			var http = new Http();
			http.Files.Add(new HttpFile { Name = "file", FileName = "test.txt" });
			Assert.Single(http.Files);
		}

		[Fact]
		public void Get_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Get();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
		}

		[Fact]
		public void Post_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Post();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Put_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Put();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Delete_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Delete();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Head_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Head();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Options_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Options();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Patch_Makes_Request_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");

			var response = http.Patch();
			Assert.NotNull(response);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Post_With_Body_To_Invalid_Url_Returns_Error()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.RequestBody = "{\"key\":\"value\"}";
			http.RequestContentType = "application/json";

			var response = http.Post();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Post_With_Parameters()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Parameters.Add(new HttpParameter { Name = "key", Value = "value" });

			var response = http.Post();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Post_With_Files()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Files.Add(new HttpFile
			{
				Name = "file",
				FileName = "test.txt",
				ContentType = "text/plain",
				Writer = (s) => { var bytes = System.Text.Encoding.UTF8.GetBytes("hello"); s.Write(bytes, 0, bytes.Length); }
			});

			var response = http.Post();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Headers()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Accept", Value = "application/json" });
			http.Headers.Add(new HttpHeader { Name = "X-Custom", Value = "test" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Cookies()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Cookies.Add(new HttpCookie { Name = "session", Value = "abc" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_UserAgent()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.UserAgent = "TestAgent";

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Credentials()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Credentials = new NetworkCredential("user", "pass");

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Timeout()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Timeout = 1000;

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Proxy()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Proxy = new WebProxy("http://nonexistent.proxy:8080");

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_FollowRedirects()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.FollowRedirects = true;
			http.MaxRedirects = 3;

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_ContentType_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Content-Type", Value = "application/json" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Get_With_Referer_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Referer", Value = "http://example.com" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Put_With_Body()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.RequestBody = "{\"update\":true}";
			http.RequestContentType = "application/json";

			var response = http.Put();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Patch_With_Body()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.RequestBody = "{\"patch\":true}";
			http.RequestContentType = "application/json";

			var response = http.Patch();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}
	}
}
