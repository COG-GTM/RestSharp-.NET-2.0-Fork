using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using RestSharp.IntegrationTests.Helpers;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class HttpClientBehaviorTests
	{
		const string BaseUrl = "http://localhost:8080/";

		static string ReadBody(HttpListenerContext ctx)
		{
			using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
				return reader.ReadToEnd();
		}

		[Fact]
		public void Sends_Custom_Headers_And_User_Agent_Without_Expect_Continue()
		{
			string custom = null, userAgent = null, expect = null, accept = null;
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				custom = ctx.Request.Headers["X-Custom"];
				userAgent = ctx.Request.UserAgent;
				expect = ctx.Request.Headers["Expect"];
				accept = ctx.Request.Headers["Accept"];
			}))
			{
				var client = new RestClient(BaseUrl) { UserAgent = "RestSharp-Test" };
				var request = new RestRequest("", Method.POST);
				request.AddHeader("X-Custom", "value");
				request.AddParameter("text/plain", "body", ParameterType.RequestBody);

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
				Assert.Equal("value", custom);
				Assert.Equal("RestSharp-Test", userAgent);
				Assert.Null(expect);
				Assert.Contains("application/json", accept);
			}
		}

		[Fact]
		public void Posts_Form_Parameters_Url_Encoded()
		{
			string contentType = null, body = null;
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				contentType = ctx.Request.ContentType;
				body = ReadBody(ctx);
			}))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddParameter("name", "John Doe");
				request.AddParameter("city", "a&b");

				client.Execute(request);

				Assert.Equal("application/x-www-form-urlencoded", contentType);
				Assert.Equal("name=John%20Doe&city=a%26b", body);
			}
		}

		[Fact]
		public void Posts_Json_Body_With_Content_Type()
		{
			string contentType = null, body = null;
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				contentType = ctx.Request.ContentType;
				body = ReadBody(ctx);
			}))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.PUT) { RequestFormat = DataFormat.Json };
				request.AddBody(new { Name = "x" });

				client.Execute(request);

				Assert.Equal("application/json", contentType);
				Assert.Contains("\"Name\": \"x\"", body);
			}
		}

		[Fact]
		public void Uploads_Multipart_File_With_Exact_Content_Length()
		{
			string contentType = null, body = null;
			long contentLength = 0;
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				contentType = ctx.Request.ContentType;
				contentLength = ctx.Request.ContentLength64;
				body = ReadBody(ctx);
			}))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("upload", Method.POST);
				request.AddParameter("field", "fieldValue");
				request.AddFile("file", Encoding.UTF8.GetBytes("file contents"), "test.txt", "text/plain");

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
				Assert.StartsWith("multipart/form-data; boundary=", contentType);
				Assert.Equal(Encoding.UTF8.GetByteCount(body), contentLength);
				Assert.Contains("Content-Disposition: form-data; name=\"field\"\r\n\r\nfieldValue", body);
				Assert.Contains("Content-Disposition: form-data; name=\"file\"; filename=\"test.txt\"\r\nContent-Type: text/plain\r\n\r\nfile contents", body);
			}
		}

		[Fact]
		public void Sends_Request_Cookies_And_Reads_Response_Cookies()
		{
			string cookieHeader = null;
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				cookieHeader = ctx.Request.Headers["Cookie"];
				ctx.Response.Headers.Add("Set-Cookie", "session=abc123; path=/; HttpOnly");
			}))
			{
				var client = new RestClient(BaseUrl) { CookieContainer = new CookieContainer() };
				var request = new RestRequest("");
				request.AddCookie("sent", "yes");

				var response = client.Execute(request);

				Assert.Equal("sent=yes", cookieHeader);
				var cookie = Assert.Single(response.Cookies);
				Assert.Equal("session", cookie.Name);
				Assert.Equal("abc123", cookie.Value);
				Assert.True(cookie.HttpOnly);
				Assert.Equal("abc123", client.CookieContainer.GetCookies(new Uri(BaseUrl))["session"].Value);
			}
		}

		[Fact]
		public void Reports_TimedOut_When_Request_Exceeds_Timeout()
		{
			using (SimpleServer.Create(BaseUrl, ctx => Thread.Sleep(2000)))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("") { Timeout = 200 };

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);
				Assert.IsType<TimeoutException>(response.ErrorException);
			}
		}

		[Fact]
		public void Reports_TimedOut_Async()
		{
			using (SimpleServer.Create(BaseUrl, ctx => Thread.Sleep(2000)))
			{
				var client = new RestClient(BaseUrl) { Timeout = 200 };
				var done = new ManualResetEventSlim();
				RestResponse result = null;

				client.ExecuteAsync(new RestRequest(""), (response, handle) =>
				{
					result = response;
					done.Set();
				});

				Assert.True(done.Wait(TimeSpan.FromSeconds(10)));
				Assert.Equal(ResponseStatus.TimedOut, result.ResponseStatus);
			}
		}

		[Fact]
		public void Abort_Cancels_Async_Request()
		{
			using (SimpleServer.Create(BaseUrl, ctx => Thread.Sleep(2000)))
			{
				var client = new RestClient(BaseUrl);
				var done = new ManualResetEventSlim();
				RestResponse<object> result = null;

				var handle = client.ExecuteAsync<object>(new RestRequest(""), (response, h) =>
				{
					result = response;
					done.Set();
				});
				handle.Abort();

				Assert.True(done.Wait(TimeSpan.FromSeconds(10)));
				Assert.True(handle.IsAborted);
				Assert.Equal(ResponseStatus.Aborted, result.ResponseStatus);
			}
		}

		[Fact]
		public void Follows_Redirects_And_Reports_Final_Uri()
		{
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				if (ctx.Request.Url.AbsolutePath == "/start")
				{
					ctx.Response.StatusCode = 302;
					ctx.Response.RedirectLocation = BaseUrl + "end";
					return;
				}
				ctx.Response.OutputStream.WriteStringUtf8("arrived");
			}))
			{
				var client = new RestClient(BaseUrl);
				var response = client.Execute(new RestRequest("start"));

				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				Assert.Equal("arrived", response.Content);
				Assert.Equal(new Uri(BaseUrl + "end"), response.ResponseUri);
			}
		}

		[Fact]
		public void Does_Not_Follow_Redirects_When_Disabled()
		{
			using (SimpleServer.Create(BaseUrl, ctx =>
			{
				ctx.Response.StatusCode = 302;
				ctx.Response.RedirectLocation = BaseUrl + "end";
			}))
			{
				var client = new RestClient(BaseUrl) { FollowRedirects = false };
				var response = client.Execute(new RestRequest("start"));

				Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
				Assert.Contains(response.Headers, h => h.Name == "Location" && (string)h.Value == BaseUrl + "end");
			}
		}

		[Fact]
		public void Reports_Error_When_Server_Unreachable()
		{
			var client = new RestClient("http://localhost:1/");
			var response = client.Execute(new RestRequest(""));

			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorException);
		}

		[Fact]
		public void Can_Use_System_Text_Json_Serializer()
		{
			string body = null;
			using (SimpleServer.Create(BaseUrl, ctx => body = ReadBody(ctx)))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST) { RequestFormat = DataFormat.Json, JsonSerializer = new SystemTextJsonSerializer() };
				request.AddBody(new { Name = "stj" });

				client.Execute(request);

				Assert.Contains("\"Name\": \"stj\"", body);
			}
		}
	}
}
