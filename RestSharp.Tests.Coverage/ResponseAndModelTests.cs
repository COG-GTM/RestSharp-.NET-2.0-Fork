using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class RestResponseTests
	{
		[Fact]
		public void RestResponse_Has_Default_Values()
		{
			var response = new RestResponse();
			Assert.NotNull(response.Headers);
			Assert.NotNull(response.Cookies);
			Assert.Equal(ResponseStatus.None, response.ResponseStatus);
		}

		[Fact]
		public void RestResponse_Content_Can_Be_Set()
		{
			var response = new RestResponse();
			response.Content = "{\"name\": \"test\"}";
			Assert.Equal("{\"name\": \"test\"}", response.Content);
		}

		[Fact]
		public void RestResponse_StatusCode_Can_Be_Set()
		{
			var response = new RestResponse();
			response.StatusCode = HttpStatusCode.OK;
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public void RestResponse_StatusDescription_Can_Be_Set()
		{
			var response = new RestResponse();
			response.StatusDescription = "OK";
			Assert.Equal("OK", response.StatusDescription);
		}

		[Fact]
		public void RestResponse_ContentType_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentType = "application/json";
			Assert.Equal("application/json", response.ContentType);
		}

		[Fact]
		public void RestResponse_ContentLength_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentLength = 1024;
			Assert.Equal(1024, response.ContentLength);
		}

		[Fact]
		public void RestResponse_ContentEncoding_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ContentEncoding = "utf-8";
			Assert.Equal("utf-8", response.ContentEncoding);
		}

		[Fact]
		public void RestResponse_RawBytes_Can_Be_Set()
		{
			var response = new RestResponse();
			var data = new byte[] { 1, 2, 3 };
			response.RawBytes = data;
			Assert.Equal(data, response.RawBytes);
		}

		[Fact]
		public void RestResponse_ResponseUri_Can_Be_Set()
		{
			var response = new RestResponse();
			var uri = new Uri("http://example.com");
			response.ResponseUri = uri;
			Assert.Equal(uri, response.ResponseUri);
		}

		[Fact]
		public void RestResponse_Server_Can_Be_Set()
		{
			var response = new RestResponse();
			response.Server = "nginx";
			Assert.Equal("nginx", response.Server);
		}

		[Fact]
		public void RestResponse_ErrorMessage_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ErrorMessage = "Connection failed";
			Assert.Equal("Connection failed", response.ErrorMessage);
		}

		[Fact]
		public void RestResponse_ErrorException_Can_Be_Set()
		{
			var response = new RestResponse();
			var ex = new Exception("test error");
			response.ErrorException = ex;
			Assert.Equal(ex, response.ErrorException);
		}

		[Fact]
		public void RestResponse_Request_Can_Be_Set()
		{
			var response = new RestResponse();
			var request = new RestRequest();
			response.Request = request;
			Assert.Equal(request, response.Request);
		}

		[Fact]
		public void RestResponse_ResponseStatus_Can_Be_Set()
		{
			var response = new RestResponse();
			response.ResponseStatus = ResponseStatus.Completed;
			Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);

			response.ResponseStatus = ResponseStatus.Error;
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);

			response.ResponseStatus = ResponseStatus.TimedOut;
			Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);

			response.ResponseStatus = ResponseStatus.Aborted;
			Assert.Equal(ResponseStatus.Aborted, response.ResponseStatus);
		}

		[Fact]
		public void GenericRestResponse_Has_Data_Property()
		{
			var response = new RestResponse<TestData>();
			response.Data = new TestData { Name = "test", Value = 42 };
			Assert.Equal("test", response.Data.Name);
			Assert.Equal(42, response.Data.Value);
		}

		[Fact]
		public void GenericRestResponse_Inherits_Base_Properties()
		{
			var response = new RestResponse<TestData>();
			response.Content = "content";
			response.StatusCode = HttpStatusCode.NotFound;
			Assert.Equal("content", response.Content);
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		private class TestData
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
	}

	public class ParameterTests
	{
		[Fact]
		public void Parameter_Properties_Can_Be_Set()
		{
			var param = new Parameter();
			param.Name = "test";
			param.Value = "value";
			param.Type = ParameterType.GetOrPost;

			Assert.Equal("test", param.Name);
			Assert.Equal("value", param.Value);
			Assert.Equal(ParameterType.GetOrPost, param.Type);
		}

		[Fact]
		public void Parameter_ToString_Returns_Name_Equals_Value()
		{
			var param = new Parameter();
			param.Name = "key";
			param.Value = "val";
			param.Type = ParameterType.GetOrPost;

			var str = param.ToString();
			Assert.Equal("key=val", str);
		}

		[Fact]
		public void Parameter_Types_Cover_All_Enum_Values()
		{
			Assert.Equal(0, (int)ParameterType.Cookie);
			Assert.Equal(1, (int)ParameterType.GetOrPost);
			Assert.Equal(2, (int)ParameterType.UrlSegment);
			Assert.Equal(3, (int)ParameterType.HttpHeader);
			Assert.Equal(4, (int)ParameterType.RequestBody);
		}
	}

	public class FileParameterTests
	{
		[Fact]
		public void Create_From_Bytes_Sets_Properties()
		{
			var data = new byte[] { 1, 2, 3, 4 };
			var fp = FileParameter.Create("file", data, "test.bin", "application/octet-stream");

			Assert.Equal("file", fp.Name);
			Assert.Equal("test.bin", fp.FileName);
			Assert.Equal("application/octet-stream", fp.ContentType);
			Assert.Equal(4, fp.ContentLength);
			Assert.NotNull(fp.Writer);
		}

		[Fact]
		public void Create_From_Bytes_Default_ContentType()
		{
			var data = new byte[] { 1, 2, 3 };
			var fp = FileParameter.Create("file", data, "test.bin");

			Assert.Null(fp.ContentType);
		}

		[Fact]
		public void Create_From_Bytes_3_Params()
		{
			var data = new byte[] { 1, 2, 3 };
			var fp = FileParameter.Create("file", data, "test.bin");

			Assert.Equal("file", fp.Name);
			Assert.Equal("test.bin", fp.FileName);
			Assert.Equal(3, fp.ContentLength);
		}

		[Fact]
		public void Writer_Writes_Correct_Data()
		{
			var data = new byte[] { 10, 20, 30 };
			var fp = FileParameter.Create("file", data, "test.bin");

			using (var stream = new System.IO.MemoryStream())
			{
				fp.Writer(stream);
				Assert.Equal(data, stream.ToArray());
			}
		}

		[Fact]
		public void FileParameter_Properties_Can_Be_Set()
		{
			var fp = new FileParameter();
			Action<System.IO.Stream> writer = s => { };
			fp.Name = "test";
			fp.FileName = "file.txt";
			fp.ContentType = "text/plain";
			fp.ContentLength = 100;
			fp.Writer = writer;

			Assert.Equal("test", fp.Name);
			Assert.Equal("file.txt", fp.FileName);
			Assert.Equal("text/plain", fp.ContentType);
			Assert.Equal(100, fp.ContentLength);
			Assert.Equal(writer, fp.Writer);
		}
	}

	public class HttpResponseTests
	{
		[Fact]
		public void HttpResponse_Has_Default_Values()
		{
			var response = new HttpResponse();
			Assert.NotNull(response.Headers);
			Assert.NotNull(response.Cookies);
			Assert.Equal(ResponseStatus.None, response.ResponseStatus);
		}

		[Fact]
		public void HttpResponse_Properties_Can_Be_Set()
		{
			var response = new HttpResponse();
			response.ContentEncoding = "utf-8";
			response.ContentLength = 4;
			response.ContentType = "text/plain";
			response.ErrorMessage = "error";
			response.ErrorException = new Exception("ex");
			response.RawBytes = System.Text.Encoding.UTF8.GetBytes("body");
			response.ResponseUri = new Uri("http://example.com");
			response.Server = "Apache";
			response.StatusCode = HttpStatusCode.OK;
			response.StatusDescription = "OK";
			response.ResponseStatus = ResponseStatus.Completed;

			Assert.Equal("body", response.Content);
			Assert.Equal("utf-8", response.ContentEncoding);
			Assert.Equal(4, response.ContentLength);
			Assert.Equal("text/plain", response.ContentType);
			Assert.Equal("error", response.ErrorMessage);
			Assert.NotNull(response.ErrorException);
			Assert.Equal(4, response.RawBytes.Length);
			Assert.Equal("http://example.com/", response.ResponseUri.ToString());
			Assert.Equal("Apache", response.Server);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Equal("OK", response.StatusDescription);
			Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
		}

		[Fact]
		public void HttpResponse_Content_Is_Lazy_Loaded_From_RawBytes()
		{
			var response = new HttpResponse();
			response.RawBytes = System.Text.Encoding.UTF8.GetBytes("hello world");
			Assert.Equal("hello world", response.Content);
		}

		[Fact]
		public void HttpResponse_Content_Returns_Empty_When_RawBytes_Null()
		{
			var response = new HttpResponse();
			// When RawBytes is null, AsString() returns empty string
			var content = response.Content;
			Assert.True(content == null || content == "");
		}
	}

	public class HttpCookieTests
	{
		[Fact]
		public void HttpCookie_Properties_Can_Be_Set()
		{
			var cookie = new HttpCookie();
			cookie.Comment = "test cookie";
			cookie.CommentUri = new Uri("http://example.com");
			cookie.Discard = true;
			cookie.Domain = "example.com";
			cookie.Expired = false;
			cookie.Expires = new DateTime(2025, 1, 1);
			cookie.HttpOnly = true;
			cookie.Name = "session";
			cookie.Path = "/";
			cookie.Port = "80";
			cookie.Secure = true;
			cookie.TimeStamp = DateTime.Now;
			cookie.Value = "abc123";
			cookie.Version = 1;

			Assert.Equal("test cookie", cookie.Comment);
			Assert.Equal("example.com", cookie.Domain);
			Assert.True(cookie.Discard);
			Assert.False(cookie.Expired);
			Assert.True(cookie.HttpOnly);
			Assert.Equal("session", cookie.Name);
			Assert.Equal("/", cookie.Path);
			Assert.True(cookie.Secure);
			Assert.Equal("abc123", cookie.Value);
			Assert.Equal(1, cookie.Version);
		}
	}

	public class RestResponseCookieTests
	{
		[Fact]
		public void RestResponseCookie_Properties_Can_Be_Set()
		{
			var cookie = new RestResponseCookie();
			cookie.Comment = "test";
			cookie.CommentUri = new Uri("http://example.com");
			cookie.Discard = true;
			cookie.Domain = "example.com";
			cookie.Expired = true;
			cookie.Expires = new DateTime(2024, 6, 1);
			cookie.HttpOnly = true;
			cookie.Name = "auth";
			cookie.Path = "/api";
			cookie.Port = "443";
			cookie.Secure = true;
			cookie.TimeStamp = DateTime.UtcNow;
			cookie.Value = "token123";
			cookie.Version = 2;

			Assert.Equal("test", cookie.Comment);
			Assert.Equal("example.com", cookie.Domain);
			Assert.True(cookie.Discard);
			Assert.True(cookie.Expired);
			Assert.True(cookie.HttpOnly);
			Assert.Equal("auth", cookie.Name);
			Assert.Equal("/api", cookie.Path);
			Assert.Equal("443", cookie.Port);
			Assert.True(cookie.Secure);
			Assert.Equal("token123", cookie.Value);
			Assert.Equal(2, cookie.Version);
		}
	}

	public class HttpHeaderTests
	{
		[Fact]
		public void HttpHeader_Properties_Can_Be_Set()
		{
			var header = new HttpHeader();
			header.Name = "Content-Type";
			header.Value = "application/json";

			Assert.Equal("Content-Type", header.Name);
			Assert.Equal("application/json", header.Value);
		}
	}

	public class HttpParameterTests
	{
		[Fact]
		public void HttpParameter_Properties_Can_Be_Set()
		{
			var param = new HttpParameter();
			param.Name = "q";
			param.Value = "search term";

			Assert.Equal("q", param.Name);
			Assert.Equal("search term", param.Value);
		}
	}

	public class HttpFileTests
	{
		[Fact]
		public void HttpFile_Properties_Can_Be_Set()
		{
			var file = new HttpFile();
			file.Name = "upload";
			file.FileName = "document.pdf";
			file.ContentType = "application/pdf";
			file.ContentLength = 1024;
			file.Writer = s => { };

			Assert.Equal("upload", file.Name);
			Assert.Equal("document.pdf", file.FileName);
			Assert.Equal("application/pdf", file.ContentType);
			Assert.Equal(1024, file.ContentLength);
			Assert.NotNull(file.Writer);
		}
	}

	public class RestRequestAsyncHandleTests
	{
		[Fact]
		public void Default_Constructor_Creates_Handle()
		{
			var handle = new RestRequestAsyncHandle();
			Assert.Null(handle.WebRequest);
		}

		[Fact]
		public void Abort_Does_Not_Throw_When_WebRequest_Is_Null()
		{
			var handle = new RestRequestAsyncHandle();
			handle.Abort(); // should not throw
		}
	}

	public class DeserializeAsAttributeTests
	{
		[Fact]
		public void DeserializeAsAttribute_Can_Be_Created()
		{
			var attr = new DeserializeAsAttribute();
			Assert.NotNull(attr);
		}

		[Fact]
		public void DeserializeAsAttribute_Name_Can_Be_Set()
		{
			var attr = new DeserializeAsAttribute { Name = "custom" };
			Assert.Equal("custom", attr.Name);
		}

		[Fact]
		public void DeserializeAsAttribute_Attribute_Can_Be_Set()
		{
			var attr = new DeserializeAsAttribute { Attribute = true };
			Assert.True(attr.Attribute);
		}
	}
}
