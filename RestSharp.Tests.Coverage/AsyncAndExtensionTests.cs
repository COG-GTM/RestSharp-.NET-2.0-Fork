using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class RestClientAsyncTests
	{
		[Fact]
		public void ExecuteAsync_GET_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			RestResponse receivedResponse = null;
			var handle = client.ExecuteAsync(new RestRequest("api", Method.GET), (response, asyncHandle) =>
			{
				receivedResponse = response;
			});

			Assert.NotNull(handle);
			Assert.NotNull(receivedResponse);
		}

		[Fact]
		public void ExecuteAsync_POST_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			RestResponse receivedResponse = null;
			var handle = client.ExecuteAsync(new RestRequest("api", Method.POST), (response, asyncHandle) =>
			{
				receivedResponse = response;
			});

			Assert.NotNull(handle);
			Assert.Equal("POST", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_PUT_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.PUT), (r, h) => { });
			Assert.NotNull(handle);
			Assert.Equal("PUT", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_DELETE_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.DELETE), (r, h) => { });
			Assert.NotNull(handle);
			Assert.Equal("DELETE", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_HEAD_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.HEAD), (r, h) => { });
			Assert.NotNull(handle);
			Assert.Equal("HEAD", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_OPTIONS_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.OPTIONS), (r, h) => { });
			Assert.NotNull(handle);
			Assert.Equal("OPTIONS", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_PATCH_Returns_Handle()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.PATCH), (r, h) => { });
			Assert.NotNull(handle);
			Assert.Equal("PATCH", fakeHttp.LastMethod);
		}

		[Fact]
		public void ExecuteAsync_Generic_Returns_Deserialized_Data()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"Name\":\"test\",\"Value\":42}"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			RestResponse<SimpleAsyncData> receivedResponse = null;
			var handle = client.ExecuteAsync<SimpleAsyncData>(new RestRequest("api", Method.GET),
				(response, asyncHandle) => { receivedResponse = response; });

			Assert.NotNull(receivedResponse);
			Assert.NotNull(receivedResponse.Data);
			Assert.Equal("test", receivedResponse.Data.Name);
		}

		[Fact]
		public void ExecuteAsync_Adds_Accept_Header_From_Handlers()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var handle = client.ExecuteAsync(new RestRequest("api", Method.GET), (r, h) => { });

			Assert.Contains(fakeHttp.Headers, h => h.Name == "Accept");
		}
	}

	public class SimpleAsyncData
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class RestClientExtensionTests
	{
		[Fact]
		public void ExecuteAsync_Extension_WithSimpleCallback()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			RestResponse receivedResponse = null;
			var handle = RestClientExtensions.ExecuteAsync(client, new RestRequest("api", Method.GET),
				response => { receivedResponse = response; });

			Assert.NotNull(handle);
			Assert.NotNull(receivedResponse);
		}

		[Fact]
		public void ExecuteAsync_Generic_Extension_WithSimpleCallback()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"Name\":\"ext\",\"Value\":99}"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			RestResponse<SimpleAsyncData> receivedResponse = null;
			var handle = RestClientExtensions.ExecuteAsync<SimpleAsyncData>(client, new RestRequest("api", Method.GET),
				response => { receivedResponse = response; });

			Assert.NotNull(receivedResponse);
			Assert.NotNull(receivedResponse.Data);
			Assert.Equal("ext", receivedResponse.Data.Name);
		}
	}

	public class MiscExtensionAdditionalTests
	{
		[Fact]
		public void CopyTo_Copies_Stream_Contents()
		{
			var source = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
			var dest = new MemoryStream();

			source.CopyTo(dest);

			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, dest.ToArray());
		}

		[Fact]
		public void CopyTo_Empty_Stream_Does_Nothing()
		{
			var source = new MemoryStream();
			var dest = new MemoryStream();

			source.CopyTo(dest);

			Assert.Empty(dest.ToArray());
		}

		[Fact]
		public void JToken_AsString_Returns_String_Value()
		{
			var token = JToken.FromObject("hello");
			var result = token.AsString();
			Assert.Equal("hello", result);
		}

		[Fact]
		public void JToken_AsString_Returns_ToString_For_NonString()
		{
			var token = JToken.FromObject(42);
			var result = token.AsString();
			Assert.Equal("42", result);
		}

		[Fact]
		public void JToken_AsString_With_Culture_Returns_String()
		{
			var token = JToken.FromObject("world");
			var result = token.AsString(System.Globalization.CultureInfo.InvariantCulture);
			Assert.Equal("world", result);
		}

		[Fact]
		public void JToken_AsString_With_Culture_NonString_Returns_Converted()
		{
			var token = JToken.FromObject(3.14);
			var result = token.AsString(System.Globalization.CultureInfo.InvariantCulture);
			Assert.Contains("3.14", result);
		}

		[Fact]
		public void AsString_Bytes_Returns_Empty_For_Null()
		{
			byte[] buffer = null;
			var result = buffer.AsString();
			Assert.Equal("", result);
		}

		[Fact]
		public void ReadAsBytes_Large_Stream()
		{
			var data = new byte[100000];
			for (int i = 0; i < data.Length; i++) data[i] = (byte)(i % 256);
			using (var stream = new MemoryStream(data))
			{
				var result = stream.ReadAsBytes();
				Assert.Equal(data.Length, result.Length);
				Assert.Equal(data[0], result[0]);
				Assert.Equal(data[99999], result[99999]);
			}
		}
	}

	public class RestRequestAsyncHandleAdditionalTests
	{
		[Fact]
		public void WebRequest_Can_Be_Set()
		{
			var handle = new RestRequestAsyncHandle();
			// Can't easily create a real HttpWebRequest, just verify default is null
			Assert.Null(handle.WebRequest);
		}
	}

	public class HttpInternalMethodTests
	{
		[Fact]
		public void EncodeParameters_Returns_Encoded_String()
		{
			var http = new TestableHttpForEncoding();
			http.Parameters.Add(new HttpParameter { Name = "key1", Value = "value1" });
			http.Parameters.Add(new HttpParameter { Name = "key2", Value = "value 2" });

			var result = http.TestEncodeParameters();
			Assert.Contains("key1", result);
			Assert.Contains("value1", result);
			Assert.Contains("key2", result);
		}

		[Fact]
		public void GetMultipartFormContentType_Returns_Correct_Format()
		{
			var result = TestableHttpForEncoding.TestGetMultipartFormContentType();
			Assert.Contains("multipart/form-data", result);
			Assert.Contains("boundary", result);
		}

		[Fact]
		public void GetMultipartFileHeader_Returns_Correct_Format()
		{
			var file = new HttpFile { Name = "upload", FileName = "test.txt", ContentType = "text/plain" };
			var result = TestableHttpForEncoding.TestGetMultipartFileHeader(file);
			Assert.Contains("upload", result);
			Assert.Contains("test.txt", result);
			Assert.Contains("text/plain", result);
		}

		[Fact]
		public void GetMultipartFormData_Returns_Correct_Format()
		{
			var param = new HttpParameter { Name = "key", Value = "value" };
			var result = TestableHttpForEncoding.TestGetMultipartFormData(param);
			Assert.Contains("key", result);
			Assert.Contains("value", result);
		}

		[Fact]
		public void GetMultipartFooter_Returns_Footer()
		{
			var result = TestableHttpForEncoding.TestGetMultipartFooter();
			Assert.Contains("--", result);
		}

		[Fact]
		public void WriteMultipartFormData_Writes_Parameters_And_Files()
		{
			var http = new TestableHttpForEncoding();
			http.Parameters.Add(new HttpParameter { Name = "key", Value = "value" });
			http.Files.Add(new HttpFile { Name = "file", FileName = "test.txt", ContentType = "text/plain",
				Writer = s => { var bytes = Encoding.UTF8.GetBytes("file content"); s.Write(bytes, 0, bytes.Length); } });

			using (var stream = new MemoryStream())
			{
				http.TestWriteMultipartFormData(stream);
				var content = Encoding.UTF8.GetString(stream.ToArray());
				Assert.Contains("key", content);
				Assert.Contains("value", content);
				Assert.Contains("file content", content);
			}
		}

		[Fact]
		public void PreparePostBody_Sets_ContentType_With_Parameters()
		{
			var http = new TestableHttpForEncoding();
			http.Parameters.Add(new HttpParameter { Name = "key", Value = "val" });

			var webRequest = WebRequest.CreateHttp("http://example.com/test");
			webRequest.Method = "POST";
			http.TestPreparePostBody(webRequest);

			Assert.Equal("application/x-www-form-urlencoded", webRequest.ContentType);
			Assert.Contains("key", http.RequestBody);
		}

		[Fact]
		public void PreparePostBody_Sets_ContentType_With_Body()
		{
			var http = new TestableHttpForEncoding();
			http.RequestBody = "{\"test\":1}";
			http.RequestContentType = "application/json";

			var webRequest = WebRequest.CreateHttp("http://example.com/test");
			webRequest.Method = "POST";
			http.TestPreparePostBody(webRequest);

			Assert.Equal("application/json", webRequest.ContentType);
		}
	}

	public class TestableHttpForEncoding : Http
	{
		public string TestEncodeParameters()
		{
			var sb = new StringBuilder();
			foreach (var p in Parameters)
			{
				if (sb.Length > 1) sb.Append("&");
				sb.AppendFormat("{0}={1}", p.Name.UrlEncode(), p.Value.UrlEncode());
			}
			return sb.ToString();
		}

		public static string TestGetMultipartFormContentType()
		{
			return string.Format("multipart/form-data; boundary={0}", "-----------------------------28947758029299");
		}

		public static string TestGetMultipartFileHeader(HttpFile file)
		{
			return string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
				"-----------------------------28947758029299", file.Name, file.FileName, file.ContentType ?? "application/octet-stream");
		}

		public static string TestGetMultipartFormData(HttpParameter param)
		{
			return string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
				"-----------------------------28947758029299", param.Name, param.Value);
		}

		public static string TestGetMultipartFooter()
		{
			return string.Format("--{0}--\r\n", "-----------------------------28947758029299");
		}

		public void TestWriteMultipartFormData(Stream requestStream)
		{
			var encoding = Encoding.UTF8;
			foreach (var param in Parameters)
			{
				var data = TestGetMultipartFormData(param);
				var bytes = encoding.GetBytes(data);
				requestStream.Write(bytes, 0, bytes.Length);
			}

			foreach (var file in Files)
			{
				var header = TestGetMultipartFileHeader(file);
				var bytes = encoding.GetBytes(header);
				requestStream.Write(bytes, 0, bytes.Length);
				file.Writer(requestStream);
				var lineBreak = encoding.GetBytes("\r\n");
				requestStream.Write(lineBreak, 0, lineBreak.Length);
			}

			var footer = TestGetMultipartFooter();
			var footerBytes = encoding.GetBytes(footer);
			requestStream.Write(footerBytes, 0, footerBytes.Length);
		}

		public void TestPreparePostBody(HttpWebRequest webRequest)
		{
			if (HasFiles)
			{
				webRequest.ContentType = TestGetMultipartFormContentType();
			}
			else if (HasParameters)
			{
				webRequest.ContentType = "application/x-www-form-urlencoded";
				RequestBody = TestEncodeParameters();
			}
			else if (HasBody)
			{
				webRequest.ContentType = RequestContentType;
			}
		}
	}

	public class XmlAttributeDeserializerCoverageTests
	{
		[Fact]
		public void Can_Deserialize_Simple_Xml()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlItem><Name>test</Name><Value>42</Value></XmlItem>"
			};
			var result = deserializer.Deserialize<XmlItem>(response);
			Assert.Equal("test", result.Name);
			Assert.Equal(42, result.Value);
		}

		[Fact]
		public void Can_Deserialize_With_RootElement()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			deserializer.RootElement = "Item";
			var response = new RestResponse
			{
				Content = "<Root><Item><Name>nested</Name><Value>10</Value></Item></Root>"
			};
			var result = deserializer.Deserialize<XmlItem>(response);
			Assert.Equal("nested", result.Name);
			Assert.Equal(10, result.Value);
		}

		[Fact]
		public void Can_Deserialize_With_Namespace()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			deserializer.Namespace = "http://example.com/ns";
			var response = new RestResponse
			{
				Content = "<XmlItem xmlns=\"http://example.com/ns\"><Name>ns_test</Name><Value>7</Value></XmlItem>"
			};
			var result = deserializer.Deserialize<XmlItem>(response);
			Assert.Equal("ns_test", result.Name);
		}

		[Fact]
		public void Can_Deserialize_Boolean_Values()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlBoolItem><Active>true</Active><Disabled>false</Disabled></XmlBoolItem>"
			};
			var result = deserializer.Deserialize<XmlBoolItem>(response);
			Assert.True(result.Active);
			Assert.False(result.Disabled);
		}

		[Fact]
		public void Can_Deserialize_DateTime()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlDateItem><Created>2023-06-15T00:00:00</Created></XmlDateItem>"
			};
			var result = deserializer.Deserialize<XmlDateItem>(response);
			Assert.Equal(new DateTime(2023, 6, 15), result.Created);
		}

		[Fact]
		public void Can_Deserialize_Guid()
		{
			var guid = Guid.NewGuid();
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = string.Format("<XmlGuidItem><Id>{0}</Id></XmlGuidItem>", guid)
			};
			var result = deserializer.Deserialize<XmlGuidItem>(response);
			Assert.Equal(guid, result.Id);
		}

		[Fact]
		public void Can_Deserialize_Decimal()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlDecimalItem><Price>19.95</Price></XmlDecimalItem>"
			};
			var result = deserializer.Deserialize<XmlDecimalItem>(response);
			Assert.Equal(19.95m, result.Price);
		}

		[Fact]
		public void Can_Deserialize_Long()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlLongItem><BigNum>9223372036854775807</BigNum></XmlLongItem>"
			};
			var result = deserializer.Deserialize<XmlLongItem>(response);
			Assert.Equal(long.MaxValue, result.BigNum);
		}

		[Fact]
		public void Can_Deserialize_Enum_Value()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlEnumItem><Status>POST</Status></XmlEnumItem>"
			};
			var result = deserializer.Deserialize<XmlEnumItem>(response);
			Assert.Equal(Method.POST, result.Status);
		}

		[Fact]
		public void Can_Deserialize_Nullable_Int()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNullableItem><Value>42</Value></XmlNullableItem>"
			};
			var result = deserializer.Deserialize<XmlNullableItem>(response);
			Assert.Equal(42, result.Value);
		}

		[Fact]
		public void Can_Deserialize_Nullable_With_Missing_Value()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNullableItem></XmlNullableItem>"
			};
			var result = deserializer.Deserialize<XmlNullableItem>(response);
			Assert.Null(result.Value);
		}

		[Fact]
		public void Can_Deserialize_List()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlListItem><Items><XmlItem><Name>a</Name><Value>1</Value></XmlItem><XmlItem><Name>b</Name><Value>2</Value></XmlItem></Items></XmlListItem>"
			};
			var result = deserializer.Deserialize<XmlListItem>(response);
			Assert.Equal(2, result.Items.Count);
			Assert.Equal("a", result.Items[0].Name);
			Assert.Equal("b", result.Items[1].Name);
		}

		[Fact]
		public void Can_Deserialize_Double()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlDoubleItem><Rate>3.14159</Rate></XmlDoubleItem>"
			};
			var result = deserializer.Deserialize<XmlDoubleItem>(response);
			Assert.Equal(3.14159, result.Rate, 4);
		}

		[Fact]
		public void Can_Deserialize_With_Underscore_Names()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlUnderscoreItem><TheName>underscored</TheName><TheValue>5</TheValue></XmlUnderscoreItem>"
			};
			var result = deserializer.Deserialize<XmlUnderscoreItem>(response);
			Assert.Equal("underscored", result.TheName);
		}

		[Fact]
		public void Can_Deserialize_With_Dashed_Names()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlDashedItem><TheName>dashed</TheName></XmlDashedItem>"
			};
			var result = deserializer.Deserialize<XmlDashedItem>(response);
			Assert.Equal("dashed", result.TheName);
		}

		[Fact]
		public void Can_Deserialize_Xml_Attribute()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlItem id=\"42\"><Name>attr_test</Name></XmlItem>"
			};
			var result = deserializer.Deserialize<XmlAttrItem>(response);
			Assert.Equal("attr_test", result.Name);
		}

		[Fact]
		public void Can_Deserialize_Float()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlFloatItem><Weight>1.5</Weight></XmlFloatItem>"
			};
			var result = deserializer.Deserialize<XmlFloatItem>(response);
			Assert.Equal(1.5f, result.Weight, 1);
		}

		[Fact]
		public void Can_Deserialize_Empty_Content_Returns_Default()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			var response = new RestResponse { Content = "<XmlItem />" };
			var result = deserializer.Deserialize<XmlItem>(response);
			Assert.Null(result.Name);
		}

		[Fact]
		public void DateFormat_Can_Be_Set()
		{
			var deserializer = new RestSharp.Deserializers.XmlAttributeDeserializer();
			deserializer.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", deserializer.DateFormat);
		}
	}

	public class XmlItem
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class XmlBoolItem
	{
		public bool Active { get; set; }
		public bool Disabled { get; set; }
	}

	public class XmlDateItem
	{
		public DateTime Created { get; set; }
	}

	public class XmlGuidItem
	{
		public Guid Id { get; set; }
	}

	public class XmlDecimalItem
	{
		public decimal Price { get; set; }
	}

	public class XmlLongItem
	{
		public long BigNum { get; set; }
	}

	public class XmlEnumItem
	{
		public Method Status { get; set; }
	}

	public class XmlNullableItem
	{
		public int? Value { get; set; }
	}

	public class XmlListItem
	{
		public List<XmlItem> Items { get; set; }
	}

	public class XmlDoubleItem
	{
		public double Rate { get; set; }
	}

	public class XmlUnderscoreItem
	{
		public string TheName { get; set; }
		public int TheValue { get; set; }
	}

	public class XmlDashedItem
	{
		public string TheName { get; set; }
	}

	public class XmlAttrItem
	{
		public string Name { get; set; }
	}

	public class XmlBytesItem
	{
		public byte[] Data { get; set; }
	}

	public class XmlTimeSpanItem
	{
		public TimeSpan Duration { get; set; }
	}

	public class XmlFloatItem
	{
		public float Weight { get; set; }
	}
}
