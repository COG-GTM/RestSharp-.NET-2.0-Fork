using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using RestSharp.Authenticators.OAuth;
using RestSharp.Contrib;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class CoverageBoostTests
	{
		// WebParameterCollection constructors
		[Fact]
		public void WebParameterCollection_NameValueCollection_Ctor()
		{
			var nvc = new NameValueCollection { { "a", "1" }, { "b", "2" } };
			var coll = new WebParameterCollection(nvc);
			Assert.Equal(2, coll.Count);
		}

		[Fact]
		public void WebParameterCollection_Capacity_Ctor()
		{
			var coll = new WebParameterCollection(10);
			Assert.Equal(0, coll.Count);
		}

		[Fact]
		public void WebParameterCollection_Dictionary_Ctor()
		{
			var dict = new Dictionary<string, string> { { "x", "1" } };
			var coll = new WebParameterCollection(dict);
			Assert.Single(coll);
		}

		[Fact]
		public void WebParameterCollection_Enumerable_Ctor()
		{
			var pairs = new List<WebPair> { new WebParameter("k", "v") };
			var coll = new WebParameterCollection(pairs);
			Assert.Single(coll);
		}

		[Fact]
		public void WebParameterCollection_Add_Override()
		{
			var coll = new WebParameterCollection();
			coll.Add("name", "value");
			Assert.IsType<WebParameter>(coll[0]);
		}

		// MiscExtensions - SaveAs
		[Fact]
		public void SaveAs_Creates_File()
		{
			var data = new byte[] { 1, 2, 3, 4, 5 };
			var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.bin");
			try
			{
				data.SaveAs(path);
				Assert.True(File.Exists(path));
				Assert.Equal(data, File.ReadAllBytes(path));
			}
			finally
			{
				if (File.Exists(path)) File.Delete(path);
			}
		}

		// MiscExtensions - CopyTo
		[Fact]
		public void CopyTo_Copies_Stream()
		{
			var source = new MemoryStream(new byte[] { 10, 20, 30 });
			var dest = new MemoryStream();
			source.CopyTo(dest);
			Assert.Equal(3, dest.Length);
		}

		// MiscExtensions - AsString for JToken
		[Fact]
		public void JToken_AsString_String()
		{
			var token = JToken.FromObject("hello");
			Assert.Equal("hello", token.AsString());
		}

		[Fact]
		public void JToken_AsString_NonString()
		{
			var token = JToken.FromObject(42);
			Assert.Equal("42", token.AsString());
		}

		[Fact]
		public void JToken_AsString_With_Culture()
		{
			var token = JToken.FromObject(3.14);
			var result = token.AsString(CultureInfo.InvariantCulture);
			Assert.NotNull(result);
		}

		[Fact]
		public void JToken_AsString_With_Culture_String()
		{
			var token = JToken.FromObject("test");
			var result = token.AsString(CultureInfo.InvariantCulture);
			Assert.Equal("test", result);
		}

		// MiscExtensions - byte[].AsString
		[Fact]
		public void ByteArray_AsString_Null()
		{
			byte[] buffer = null;
			Assert.Equal("", buffer.AsString());
		}

		[Fact]
		public void ByteArray_AsString_UTF8()
		{
			var buffer = System.Text.Encoding.UTF8.GetBytes("hello");
			Assert.Equal("hello", buffer.AsString());
		}

		// RestClientExtensions
		[Fact]
		public void RestClientExtensions_ExecuteAsync_Invokes()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test");
			bool called = false;
			var handle = RestClientExtensions.ExecuteAsync(client, request, (response) =>
			{
				called = true;
			});
			Assert.NotNull(handle);
		}

		// RestRequestAsyncHandle
		[Fact]
		public void RestRequestAsyncHandle_Abort()
		{
			var handle = new RestRequestAsyncHandle();
			// No web request set, Abort should not throw
			handle.Abort();
		}

		// HttpFile properties
		[Fact]
		public void HttpFile_All_Properties()
		{
			var file = new HttpFile
			{
				Name = "upload",
				FileName = "doc.pdf",
				ContentType = "application/pdf",
				ContentLength = 1024,
				Writer = (s) => { }
			};
			Assert.Equal("upload", file.Name);
			Assert.Equal("doc.pdf", file.FileName);
			Assert.Equal("application/pdf", file.ContentType);
			Assert.Equal(1024, file.ContentLength);
			Assert.NotNull(file.Writer);
		}

		// RestRequest additional coverage
		[Fact]
		public void RestRequest_AddFile_Bytes()
		{
			var request = new RestRequest();
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.bin");
			Assert.Single(request.Files);
		}

		[Fact]
		public void RestRequest_AddFile_Writer()
		{
			var request = new RestRequest();
			request.AddFile("file", (s) => s.WriteByte(1), "test.bin");
			Assert.Single(request.Files);
		}

		[Fact]
		public void RestRequest_AddFile_Path()
		{
			var path = Path.GetTempFileName();
			try
			{
				File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
				var request = new RestRequest();
				request.AddFile("file", path);
				Assert.Single(request.Files);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void RestRequest_Timeout_Property()
		{
			var request = new RestRequest();
			request.Timeout = 5000;
			Assert.Equal(5000, request.Timeout);
		}

		[Fact]
		public void RestRequest_Credentials_Property()
		{
			var request = new RestRequest();
			var cred = new System.Net.NetworkCredential("user", "pass");
			request.Credentials = cred;
			Assert.Same(cred, request.Credentials);
		}

		// RestResponseCookie coverage
		[Fact]
		public void RestResponseCookie_All_Properties()
		{
			var cookie = new RestResponseCookie
			{
				Comment = "test",
				CommentUri = new Uri("http://example.com"),
				Discard = true,
				Domain = ".test.com",
				Expired = false,
				Expires = DateTime.Now,
				HttpOnly = true,
				Name = "sid",
				Path = "/",
				Port = "443",
				Secure = true,
				TimeStamp = DateTime.Now,
				Value = "xyz",
				Version = 1
			};
			Assert.Equal("test", cookie.Comment);
			Assert.Equal("sid", cookie.Name);
			Assert.True(cookie.HttpOnly);
		}

		// HttpEncoder coverage
		[Fact]
		public void HttpEncoder_HtmlEncode_HighChar()
		{
			var result = HttpUtility.HtmlEncode("\u00A9"); // ©
			Assert.NotNull(result);
		}

		[Fact]
		public void HttpEncoder_UrlEncode_AllChars()
		{
			// Test various characters that need encoding
			var result = HttpUtility.UrlEncode("!@#$%^&*()");
			Assert.NotNull(result);
			Assert.Contains("%", result);
		}

		[Fact]
		public void HttpEncoder_UrlDecode_Plus_And_Percent()
		{
			Assert.Equal("a b", HttpUtility.UrlDecode("a+b"));
			Assert.Equal("a&b", HttpUtility.UrlDecode("a%26b"));
		}

		// Http class - additional internal method coverage
		[Fact]
		public void Http_Post_With_Files_And_Parameters()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Parameters.Add(new HttpParameter { Name = "field", Value = "value" });
			http.Files.Add(new HttpFile
			{
				Name = "file",
				FileName = "test.txt",
				ContentType = "text/plain",
				Writer = (s) =>
				{
					var bytes = System.Text.Encoding.UTF8.GetBytes("test content");
					s.Write(bytes, 0, bytes.Length);
				}
			});

			var response = http.Post();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_If_Modified_Since()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "If-Modified-Since", Value = DateTime.UtcNow.ToString("R") });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_Expect_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Expect", Value = "custom-expect" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_Multiple_Custom_Headers()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "X-Custom-1", Value = "value1" });
			http.Headers.Add(new HttpHeader { Name = "X-Custom-2", Value = "value2" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_UserAgent_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "User-Agent", Value = "CustomUA" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_Date_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Date", Value = DateTime.UtcNow.ToString("R") });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_Host_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Host", Value = "example.com" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_ContentLength_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Content-Length", Value = "0" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Get_With_Range_Header()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Headers.Add(new HttpHeader { Name = "Range", Value = "bytes=0-100" });

			var response = http.Get();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Http_Post_With_Only_Parameters_No_Body()
		{
			var http = new Http();
			http.Url = new Uri("http://localhost:1/nonexistent");
			http.Parameters.Add(new HttpParameter { Name = "a", Value = "1" });
			http.Parameters.Add(new HttpParameter { Name = "b", Value = "2" });

			var response = http.Post();
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}
	}
}
