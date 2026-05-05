using System;
using System.Net;
using System.Text;
using Xunit;

namespace RestSharp.Tests
{
	public class RestResponseTests
	{
		[Fact]
		public void Default_Constructor_Initializes_Collections()
		{
			var response = new RestResponse();
			Assert.NotNull(response.Headers);
			Assert.Empty(response.Headers);
			Assert.NotNull(response.Cookies);
			Assert.Empty(response.Cookies);
		}

		[Fact]
		public void Content_From_RawBytes()
		{
			var response = new RestResponse();
			response.RawBytes = Encoding.UTF8.GetBytes("Hello World");
			Assert.Equal("Hello World", response.Content);
		}

		[Fact]
		public void Content_Returns_Empty_When_RawBytes_Null()
		{
			var response = new RestResponse();
			response.RawBytes = null;
			Assert.Equal("", response.Content);
		}

		[Fact]
		public void Content_Can_Be_Set_Directly()
		{
			var response = new RestResponse();
			response.Content = "direct content";
			Assert.Equal("direct content", response.Content);
		}

		[Fact]
		public void StatusCode_Can_Be_Set()
		{
			var response = new RestResponse();
			response.StatusCode = HttpStatusCode.OK;
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public void StatusDescription_Can_Be_Set()
		{
			var response = new RestResponse();
			response.StatusDescription = "OK";
			Assert.Equal("OK", response.StatusDescription);
		}

		[Fact]
		public void ContentType_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentType = "application/json";
			Assert.Equal("application/json", response.ContentType);
		}

		[Fact]
		public void ContentLength_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentLength = 1024;
			Assert.Equal(1024, response.ContentLength);
		}

		[Fact]
		public void ContentEncoding_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentEncoding = "gzip";
			Assert.Equal("gzip", response.ContentEncoding);
		}

		[Fact]
		public void ResponseUri_Can_Be_Set()
		{
			var response = new RestResponse();
			var uri = new Uri("http://example.com/resource");
			response.ResponseUri = uri;
			Assert.Equal(uri, response.ResponseUri);
		}

		[Fact]
		public void Server_Can_Be_Set()
		{
			var response = new RestResponse();
			response.Server = "nginx";
			Assert.Equal("nginx", response.Server);
		}

		[Fact]
		public void ResponseStatus_Defaults_To_None()
		{
			var response = new RestResponse();
			Assert.Equal(ResponseStatus.None, response.ResponseStatus);
		}

		[Fact]
		public void ResponseStatus_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ResponseStatus = ResponseStatus.Completed;
			Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
		}

		[Fact]
		public void ErrorMessage_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ErrorMessage = "Something went wrong";
			Assert.Equal("Something went wrong", response.ErrorMessage);
		}

		[Fact]
		public void ErrorException_Can_Be_Set()
		{
			var response = new RestResponse();
			var ex = new InvalidOperationException("test");
			response.ErrorException = ex;
			Assert.Same(ex, response.ErrorException);
		}

		[Fact]
		public void Request_Can_Be_Set()
		{
			var response = new RestResponse();
			var request = new RestRequest("/test");
			response.Request = request;
			Assert.Same(request, response.Request);
		}

		[Fact]
		public void Generic_RestResponse_Cast_Operator()
		{
			var response = new RestResponse();
			response.Content = "test";
			response.StatusCode = HttpStatusCode.OK;
			response.ContentType = "text/plain";
			response.ContentEncoding = "utf-8";
			response.ContentLength = 4;
			response.Server = "test-server";
			response.ResponseStatus = ResponseStatus.Completed;
			response.ResponseUri = new Uri("http://example.com");
			response.StatusDescription = "OK";
			response.RawBytes = Encoding.UTF8.GetBytes("test");

			var genericResponse = (RestResponse<string>)response;
			Assert.Equal(HttpStatusCode.OK, genericResponse.StatusCode);
			Assert.Equal("text/plain", genericResponse.ContentType);
			Assert.Equal("utf-8", genericResponse.ContentEncoding);
			Assert.Equal(4, genericResponse.ContentLength);
			Assert.Equal("test-server", genericResponse.Server);
			Assert.Equal(ResponseStatus.Completed, genericResponse.ResponseStatus);
			Assert.Equal("OK", genericResponse.StatusDescription);
		}

		[Fact]
		public void Generic_RestResponse_Data_Can_Be_Set()
		{
			var response = new RestResponse<int>();
			response.Data = 42;
			Assert.Equal(42, response.Data);
		}

		[Fact]
		public void RestResponseCookie_Properties()
		{
			var cookie = new RestResponseCookie();
			cookie.Name = "session";
			cookie.Value = "abc123";
			cookie.Domain = "example.com";
			cookie.Path = "/";
			cookie.HttpOnly = true;
			cookie.Secure = true;
			cookie.Expired = false;
			cookie.Expires = new DateTime(2025, 12, 31);
			cookie.Discard = false;
			cookie.Port = "443";
			cookie.Version = 1;
			cookie.Comment = "Session cookie";
			cookie.CommentUri = new Uri("http://example.com/cookies");
			cookie.TimeStamp = DateTime.UtcNow;

			Assert.Equal("session", cookie.Name);
			Assert.Equal("abc123", cookie.Value);
			Assert.Equal("example.com", cookie.Domain);
			Assert.Equal("/", cookie.Path);
			Assert.True(cookie.HttpOnly);
			Assert.True(cookie.Secure);
			Assert.False(cookie.Expired);
			Assert.Equal(1, cookie.Version);
		}
	}
}
