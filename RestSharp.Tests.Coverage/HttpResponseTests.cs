using System;
using System.Net;
using System.Text;
using Xunit;

namespace RestSharp.Tests
{
	public class HttpResponseTests
	{
		[Fact]
		public void Default_Constructor_Initializes_Collections()
		{
			var response = new HttpResponse();
			Assert.NotNull(response.Headers);
			Assert.NotNull(response.Cookies);
		}

		[Fact]
		public void StatusCode_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.StatusCode = HttpStatusCode.OK;
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public void StatusDescription_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.StatusDescription = "Not Found";
			Assert.Equal("Not Found", response.StatusDescription);
		}

		[Fact]
		public void ContentType_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ContentType = "text/html";
			Assert.Equal("text/html", response.ContentType);
		}

		[Fact]
		public void ContentLength_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ContentLength = 2048;
			Assert.Equal(2048, response.ContentLength);
		}

		[Fact]
		public void ContentEncoding_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ContentEncoding = "utf-8";
			Assert.Equal("utf-8", response.ContentEncoding);
		}

		[Fact]
		public void RawBytes_Can_Be_Set()
		{
			var response = new HttpResponse();
			var data = new byte[] { 1, 2, 3 };
			response.RawBytes = data;
			Assert.Equal(data, response.RawBytes);
		}

		[Fact]
		public void Content_Lazy_Loaded_From_RawBytes()
		{
			var response = new HttpResponse();
			response.RawBytes = Encoding.UTF8.GetBytes("Hello World");
			Assert.Equal("Hello World", response.Content);
		}

		[Fact]
		public void Content_Returns_Empty_When_RawBytes_Null()
		{
			var response = new HttpResponse();
			response.RawBytes = null;
			Assert.Equal("", response.Content);
		}

		[Fact]
		public void ResponseUri_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ResponseUri = new Uri("http://example.com");
			Assert.Equal("http://example.com/", response.ResponseUri.ToString());
		}

		[Fact]
		public void Server_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.Server = "Apache";
			Assert.Equal("Apache", response.Server);
		}

		[Fact]
		public void ResponseStatus_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ResponseStatus = ResponseStatus.Error;
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void ErrorMessage_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ErrorMessage = "Connection refused";
			Assert.Equal("Connection refused", response.ErrorMessage);
		}

		[Fact]
		public void ErrorException_Can_Be_Set()
		{
			var response = new HttpResponse();
			var ex = new Exception("test");
			response.ErrorException = ex;
			Assert.Same(ex, response.ErrorException);
		}
	}
}
