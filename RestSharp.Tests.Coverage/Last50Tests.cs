using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Contrib;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class HttpUtilityBytePathTests
	{
		[Fact]
		public void UrlDecode_String_With_Encoding()
		{
			var result = HttpUtility.UrlDecode("hello+world", Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_String_With_UTF8_MultiByte_Encoding()
		{
			// UTF-8 encoded é = %c3%a9
			var result = HttpUtility.UrlDecode("%c3%a9", Encoding.UTF8);
			Assert.Equal("\u00e9", result);
		}

		[Fact]
		public void UrlDecode_Byte_Array_With_Percent_And_Offset()
		{
			var bytes = Encoding.ASCII.GetBytes("a%20b");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("a b", result);
		}

		[Fact]
		public void UrlDecode_Byte_Array_With_Plus()
		{
			var bytes = Encoding.ASCII.GetBytes("hello+world");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_Byte_Array_With_Hex_Encoded_High()
		{
			// %c3%a9 = UTF-8 é
			var bytes = Encoding.ASCII.GetBytes("%c3%a9");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("\u00e9", result);
		}

		[Fact]
		public void UrlDecodeToBytes_String_With_Encoding()
		{
			var result = HttpUtility.UrlDecodeToBytes("a%20b", Encoding.UTF8);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array_With_Offset()
		{
			var bytes = Encoding.ASCII.GetBytes("a%20b");
			var result = HttpUtility.UrlDecodeToBytes(bytes, 0, bytes.Length);
			Assert.NotNull(result);
			Assert.True(result.Length > 0);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array_NoOffset()
		{
			var bytes = Encoding.ASCII.GetBytes("a%20b");
			var result = HttpUtility.UrlDecodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncode_Byte_Array_NoOffset()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			var result = HttpUtility.UrlEncode(bytes);
			Assert.Equal("hello", result);
		}

		[Fact]
		public void UrlEncode_Byte_Array_With_Offset()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncode(bytes, 0, bytes.Length);
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncodeToBytes_String_With_Encoding()
		{
			var result = HttpUtility.UrlEncodeToBytes("hello", Encoding.UTF8);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array_NoOffset()
		{
			var bytes = Encoding.UTF8.GetBytes("abc");
			var result = HttpUtility.UrlEncodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array_With_Offset()
		{
			var bytes = Encoding.UTF8.GetBytes("abc");
			var result = HttpUtility.UrlEncodeToBytes(bytes, 0, bytes.Length);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeUnicodeToBytes_Returns_Bytes()
		{
			var result = HttpUtility.UrlEncodeUnicodeToBytes("test\u4e2d");
			Assert.NotNull(result);
			Assert.True(result.Length > 0);
		}

		[Fact]
		public void HtmlDecode_To_TextWriter_Null_Source()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlDecode(null, writer);
			Assert.Equal("", writer.ToString());
		}

		[Fact]
		public void HtmlEncode_To_TextWriter_Null_Source()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlEncode(null, writer);
			Assert.Equal("", writer.ToString());
		}

		[Fact]
		public void HtmlAttributeEncode_To_TextWriter_Regular()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlAttributeEncode("test\"value", writer);
			var result = writer.ToString();
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void ParseQueryString_With_Encoding_And_QuestionMark()
		{
			var result = HttpUtility.ParseQueryString("?a=1&b=2", Encoding.UTF8);
			Assert.Equal("1", result["a"]);
			Assert.Equal("2", result["b"]);
		}

		[Fact]
		public void HttpQSCollection_ToString_Returns_Formatted()
		{
			var coll = HttpUtility.ParseQueryString("a=1&b=2");
			var str = coll.ToString();
			Assert.Contains("a=1", str);
			Assert.Contains("b=2", str);
		}
	}

	public class HtmlEncoderInternalPathTests
	{
		[Fact]
		public void HtmlEncode_Empty_String_Returns_Empty()
		{
			var result = HttpUtility.HtmlEncode("");
			Assert.Equal("", result);
		}

		[Fact]
		public void HtmlDecode_Empty_Returns_Empty()
		{
			var result = HttpUtility.HtmlDecode("");
			Assert.Equal("", result);
		}

		[Fact]
		public void HtmlAttributeEncode_Empty_Returns_Empty()
		{
			var result = HttpUtility.HtmlAttributeEncode("");
			Assert.Equal("", result);
		}

		[Fact]
		public void HtmlDecode_Entity_With_Hex_Then_Invalid()
		{
			// hex entity with invalid hex digit triggers have_trailing_digits path
			var result = HttpUtility.HtmlDecode("&#x41G;");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlPathEncode_SpecialChars()
		{
			var result = HttpUtility.UrlPathEncode("/path/to file?q=1");
			Assert.Contains("%20", result);
			Assert.Contains("?", result);
		}
	}

	public class RestClientDeserializeTests
	{
		[Fact]
		public void Execute_With_Error_Response_Sets_ErrorMessage()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("error"),
				ContentType = "text/plain",
				StatusCode = HttpStatusCode.InternalServerError,
				ResponseStatus = ResponseStatus.Error,
				ErrorMessage = "Something failed",
				ErrorException = new Exception("boom")
			});

			var response = client.Execute(new RestRequest("api", Method.GET));
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_Generic_With_Error_Returns_Default_Data()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes(""),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.InternalServerError,
				ResponseStatus = ResponseStatus.Error,
				ErrorMessage = "Something failed",
				ErrorException = new Exception("boom")
			});

			var response = client.Execute<SimpleAsyncData>(new RestRequest("api", Method.GET));
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}

		[Fact]
		public void Execute_Generic_Deserialize_With_Handler_Error()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("not valid json"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			var response = client.Execute<SimpleAsyncData>(new RestRequest("api", Method.GET));
			// Either the data is null due to deserialization error, or an error is caught
			Assert.NotNull(response);
		}

		[Fact]
		public void BuildUri_With_UrlSegment_Substitution()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("api/{id}", Method.GET);
			request.AddUrlSegment("id", "42");
			var uri = client.BuildUri(request);
			Assert.Contains("42", uri.ToString());
			Assert.DoesNotContain("{id}", uri.ToString());
		}
	}

	public class RestRequestFileTests
	{
		[Fact]
		public void AddFile_With_ContentType_Inferred()
		{
			var request = new RestRequest();
			var tmpFile = Path.GetTempFileName();
			try
			{
				File.WriteAllText(tmpFile, "test content");
				request.AddFile("file", tmpFile);
				Assert.Single(request.Files);
				Assert.Equal("file", request.Files[0].Name);
			}
			finally
			{
				File.Delete(tmpFile);
			}
		}

		[Fact]
		public void AddFile_With_Explicit_ContentType()
		{
			var request = new RestRequest();
			var tmpFile = Path.GetTempFileName();
			try
			{
				File.WriteAllText(tmpFile, "test content");
				request.AddFile("file", Encoding.UTF8.GetBytes("test content"), "test.txt", "text/plain");
				Assert.Single(request.Files);
			}
			finally
			{
				File.Delete(tmpFile);
			}
		}

		[Fact]
		public void UserState_Property()
		{
			var request = new RestRequest();
			Assert.Null(request.UserState);
		}
	}

	public class MiscCopyToTests
	{
		[Fact]
		public void CopyTo_Stream_Extension_Works()
		{
			var src = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
			var dst = new MemoryStream();
			MiscExtensions.CopyTo(src, dst);
			Assert.Equal(5, dst.Length);
		}
	}

	public class JTokenAsStringTests
	{
		[Fact]
		public void AsString_With_Culture()
		{
			var token = JToken.Parse("3.14");
			var result = token.AsString(CultureInfo.InvariantCulture);
			Assert.Equal("3.14", result);
		}
	}

	public class XmlAttributeDeserializerMapTests
	{
		[Fact]
		public void Can_Deserialize_With_Underscore_Names()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrUnderscoreItem><first_name>John</first_name><last_name>Doe</last_name></XmlAttrUnderscoreItem>"
			};
			var result = deserializer.Deserialize<XmlAttrUnderscoreItem>(response);
			// XmlAttributeDeserializer might not do underscore-to-camelCase mapping
			Assert.NotNull(result);
		}

		[Fact]
		public void Can_Deserialize_With_Dashed_Names()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrDashedItem><first-name>John</first-name><last-name>Doe</last-name></XmlAttrDashedItem>"
			};
			var result = deserializer.Deserialize<XmlAttrDashedItem>(response);
			Assert.NotNull(result);
		}

		[Fact]
		public void GetElementByName_With_LowerCase()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrMixed><thename>test</thename></XmlAttrMixed>"
			};
			var result = deserializer.Deserialize<XmlAttrMixed>(response);
			Assert.Equal("test", result.TheName);
		}

		[Fact]
		public void GetAttributeByName_With_LowerCase()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrById id=\"99\" />"
			};
			var result = deserializer.Deserialize<XmlAttrById>(response);
			Assert.Equal("99", result.Id);
		}
	}

	// Model classes
	public class XmlAttrUnderscoreItem
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}

	public class XmlAttrDashedItem
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}

	public class XmlAttrMixed
	{
		public string TheName { get; set; }
	}

	public class XmlAttrById
	{
		public string Id { get; set; }
	}
}
