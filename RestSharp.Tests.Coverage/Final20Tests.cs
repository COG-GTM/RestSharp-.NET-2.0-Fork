using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Contrib;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class RestRequestAddFilePathTests
	{
		[Fact]
		public void AddFile_With_Path_Creates_FileParameter()
		{
			var request = new RestRequest();
			var tmpFile = Path.GetTempFileName();
			try
			{
				File.WriteAllBytes(tmpFile, new byte[] { 1, 2, 3 });
				request.AddFile("upload", tmpFile);
				Assert.Single(request.Files);
				Assert.Equal("upload", request.Files[0].Name);
				// Verify the writer can produce the data
				using (var ms = new MemoryStream())
				{
					request.Files[0].Writer(ms);
					Assert.Equal(3, ms.Length);
				}
			}
			finally
			{
				File.Delete(tmpFile);
			}
		}

		[Fact]
		public void AddFile_With_Action_No_ContentType()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => s.Write(new byte[] { 10, 20 }, 0, 2);
			request.AddFile("f", writer, "data.bin");
			Assert.Single(request.Files);
			Assert.Equal("data.bin", request.Files[0].FileName);
		}
	}

	public class XmlSerializerConstructorTests
	{
		[Fact]
		public void Constructor_With_Namespace_Sets_Property()
		{
			var serializer = new XmlSerializer("http://myns.com");
			Assert.Equal("http://myns.com", serializer.Namespace);
		}

		[Fact]
		public void Serialize_With_Enum_Property()
		{
			var serializer = new XmlSerializer();
			var obj = new ObjWithEnumFinal { Status = Method.POST };
			var result = serializer.Serialize(obj);
			Assert.Contains("POST", result);
		}
	}

	public class JsonSerializerConstructorTests
	{
		[Fact]
		public void Constructor_With_Custom_JsonSerializer()
		{
			var jsonSerializer = new Newtonsoft.Json.JsonSerializer();
			jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
			var serializer = new RestSharp.Serializers.JsonSerializer(jsonSerializer);
			Assert.NotNull(serializer.ContentType);
		}
	}

	public class RestRequestAsyncHandleConstructorTests
	{
		[Fact]
		public void Constructor_With_WebRequest_Sets_Property()
		{
			var webReq = (HttpWebRequest)WebRequest.Create("http://example.com");
			var handle = new RestRequestAsyncHandle(webReq);
			Assert.Same(webReq, handle.WebRequest);
		}
	}

	public class RestClientHandlerAndBuildUriTests
	{
		[Fact]
		public void GetHandler_Matches_ContentType_With_Charset()
		{
			var client = new RestClient("http://example.com");
			// Should handle content-type with charset parameter
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"Name\":\"test\"}"),
				ContentType = "application/json; charset=utf-8",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});
			var response = client.Execute<SimpleAsyncData>(new RestRequest("api", Method.GET));
			Assert.NotNull(response);
			Assert.Equal("test", response.Data.Name);
		}

		[Fact]
		public void BuildUri_With_BaseUrl_Query_String()
		{
			var client = new RestClient("http://example.com?key=value");
			var request = new RestRequest("api", Method.GET);
			var uri = client.BuildUri(request);
			Assert.Contains("key=value", uri.ToString());
		}

		[Fact]
		public void ConfigureHttp_With_DefaultParameters()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.AddDefaultHeader("X-Custom", "custom-value");

			var request = new RestRequest("api", Method.GET);
			client.Execute(request);

			Assert.Contains(fakeHttp.Headers, h => h.Name == "X-Custom");
		}
	}

	public class RestClientAsyncAdditionalTests
	{
		[Fact]
		public void ExecuteAsync_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			RestResponse receivedResponse = null;
			var handle = client.ExecuteAsync(request, (resp, hdl) => { receivedResponse = (RestResponse)resp; });
			Assert.NotNull(handle);
		}
	}

	public class StringExtensionHtmlAttrEncodeTests
	{
		[Fact]
		public void HtmlAttributeEncode_Extension_Encodes_Quotes()
		{
			var result = "test\"value".HtmlAttributeEncode();
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void ParseJsonDate_FormattedDate_Fallback()
		{
			// A date in standard DateTime format triggers ParseFormattedDate path
			var result = "2023-06-15T12:30:00".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2023, result.Year);
			Assert.Equal(6, result.Month);
		}
	}

	public class HttpUtilityFinalSmallTests
	{
		[Fact]
		public void UrlDecode_With_Encoding_Percent_UFormat()
		{
			// %u format in UrlDecode with explicit encoding
			var result = HttpUtility.UrlDecode("%u0041", Encoding.UTF8);
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlAttributeEncode_TextWriter_Empty()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlAttributeEncode("", writer);
			Assert.Equal("", writer.ToString());
		}

		[Fact]
		public void ParseQueryString_NoQuestion_EmptyString_Key()
		{
			var result = HttpUtility.ParseQueryString("key_only");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Returns_Correct_Bytes()
		{
			var result = HttpUtility.UrlEncodeToBytes("a b");
			Assert.NotNull(result);
			Assert.True(result.Length > 0);
		}
	}

	public class JsonDeserializerCreateAndMapTests
	{
		[Fact]
		public void Deserialize_Nested_Object_Triggers_CreateAndMap()
		{
			var deserializer = new JsonDeserializer();
			var response = new RestResponse
			{
				Content = "{\"Child\":{\"Name\":\"nested\",\"Value\":5}}"
			};
			var result = deserializer.Deserialize<JsonNestedObj>(response);
			Assert.NotNull(result.Child);
			Assert.Equal("nested", result.Child.Name);
			Assert.Equal(5, result.Child.Value);
		}

		[Fact]
		public void Deserialize_List_Of_Objects()
		{
			var deserializer = new JsonDeserializer();
			var response = new RestResponse
			{
				Content = "[{\"Name\":\"a\",\"Value\":1},{\"Name\":\"b\",\"Value\":2}]"
			};
			var result = deserializer.Deserialize<List<JsonChildObj>>(response);
			Assert.Equal(2, result.Count);
			Assert.Equal("a", result[0].Name);
		}
	}

	// Model classes
	public class ObjWithEnumFinal { public Method Status { get; set; } }
	public class JsonNestedObj { public JsonChildObj Child { get; set; } }
	public class JsonChildObj { public string Name { get; set; } public int Value { get; set; } }
}
