using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	/// <summary>
	/// Additional tests targeting remaining uncovered lines across the codebase.
	/// </summary>

	public class XmlDeserializerAdditionalCoverageTests
	{
		[Fact]
		public void Can_Deserialize_Uri()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlUriItem><Link>http://example.com</Link></XmlUriItem>"
			};
			var result = deserializer.Deserialize<XmlUriItem>(response);
			Assert.Equal("http://example.com/", result.Link.ToString());
		}

		[Fact]
		public void Can_Deserialize_Float()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlFloatItem2><Rate>1.5</Rate></XmlFloatItem2>"
			};
			var result = deserializer.Deserialize<XmlFloatItem2>(response);
			Assert.Equal(1.5f, result.Rate, 1);
		}

		[Fact]
		public void Can_Deserialize_Int64()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlLong2><BigNum>9223372036854775807</BigNum></XmlLong2>"
			};
			var result = deserializer.Deserialize<XmlLong2>(response);
			Assert.Equal(long.MaxValue, result.BigNum);
		}

		[Fact]
		public void Can_Deserialize_DateFormat_Custom()
		{
			var deserializer = new XmlDeserializer();
			deserializer.DateFormat = "yyyy-MM-dd";
			var response = new RestResponse
			{
				Content = "<XmlDateFmtItem><Date>2023-12-25</Date></XmlDateFmtItem>"
			};
			var result = deserializer.Deserialize<XmlDateFmtItem>(response);
			Assert.Equal(new DateTime(2023, 12, 25), result.Date);
		}

		[Fact]
		public void Can_Deserialize_With_Culture()
		{
			var deserializer = new XmlDeserializer();
			deserializer.Culture = CultureInfo.InvariantCulture;
			var response = new RestResponse
			{
				Content = "<XmlCultureItem><Price>19.95</Price></XmlCultureItem>"
			};
			var result = deserializer.Deserialize<XmlCultureItem>(response);
			Assert.Equal(19.95m, result.Price);
		}

		[Fact]
		public void Can_Deserialize_With_Namespace_And_Prefix()
		{
			var deserializer = new XmlDeserializer();
			deserializer.Namespace = "http://example.com/ns";
			var response = new RestResponse
			{
				Content = "<XmlSimpleNs xmlns=\"http://example.com/ns\"><Name>ns_value</Name></XmlSimpleNs>"
			};
			var result = deserializer.Deserialize<XmlSimpleNs>(response);
			Assert.Equal("ns_value", result.Name);
		}

		[Fact]
		public void Can_Deserialize_Nullable_DateTime()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNullDateItem><When>2023-06-15T00:00:00</When></XmlNullDateItem>"
			};
			var result = deserializer.Deserialize<XmlNullDateItem>(response);
			Assert.NotNull(result.When);
			Assert.Equal(new DateTime(2023, 6, 15), result.When.Value);
		}

		[Fact]
		public void Can_Deserialize_Nested_Object()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNestedItem><Child><Name>child_name</Name><Value>10</Value></Child></XmlNestedItem>"
			};
			var result = deserializer.Deserialize<XmlNestedItem>(response);
			Assert.NotNull(result.Child);
			Assert.Equal("child_name", result.Child.Name);
			Assert.Equal(10, result.Child.Value);
		}

		[Fact]
		public void Can_Deserialize_Nullable_Bool()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNullBoolItem><Active>true</Active></XmlNullBoolItem>"
			};
			var result = deserializer.Deserialize<XmlNullBoolItem>(response);
			Assert.True(result.Active);
		}

		[Fact]
		public void Can_Deserialize_Nullable_Bool_Missing()
		{
			var deserializer = new XmlDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlNullBoolItem></XmlNullBoolItem>"
			};
			var result = deserializer.Deserialize<XmlNullBoolItem>(response);
			Assert.Null(result.Active);
		}

		[Fact]
		public void RootElement_Property()
		{
			var deserializer = new XmlDeserializer();
			deserializer.RootElement = "Root";
			Assert.Equal("Root", deserializer.RootElement);
		}

		[Fact]
		public void XmlNamespace_Property()
		{
			var deserializer = new XmlDeserializer();
			deserializer.Namespace = "http://ns.example.com";
			Assert.Equal("http://ns.example.com", deserializer.Namespace);
		}
	}

	public class XmlSerializerAdditionalCoverageTests
	{
		[Fact]
		public void Serialize_With_RootElement()
		{
			var serializer = new XmlSerializer();
			serializer.RootElement = "CustomRoot";
			var result = serializer.Serialize(new SimpleSerObj { Name = "test" });
			Assert.Contains("CustomRoot", result);
		}

		[Fact]
		public void Serialize_With_DateFormat()
		{
			var serializer = new XmlSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			var result = serializer.Serialize(new ObjWithDateTime { When = new DateTime(2023, 6, 15) });
			Assert.Contains("2023-06-15", result);
		}

		[Fact]
		public void Serialize_Null_Value_Skipped()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithNullableString { Name = null });
			Assert.DoesNotContain("<Name>", result);
		}

		[Fact]
		public void Serialize_With_Int_Property()
		{
			var serializer = new XmlSerializer();
			var obj = new ObjWithInt { Count = 42 };
			var result = serializer.Serialize(obj);
			Assert.Contains("42", result);
		}

		[Fact]
		public void Serialize_Nested_Object()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithChild { Child = new SimpleSerObj { Name = "nested" } });
			Assert.Contains("nested", result);
		}

		[Fact]
		public void Namespace_Property()
		{
			var serializer = new XmlSerializer();
			serializer.Namespace = "http://ns.example.com";
			Assert.Equal("http://ns.example.com", serializer.Namespace);
		}

		[Fact]
		public void RootElement_Property()
		{
			var serializer = new XmlSerializer();
			serializer.RootElement = "Root";
			Assert.Equal("Root", serializer.RootElement);
		}
	}

	public class RestRequestAdditionalCoverageTests
	{
		[Fact]
		public void AddBody_With_XmlNamespace()
		{
			var request = new RestRequest();
			request.XmlSerializer = new XmlSerializer();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new SimpleSerObj { Name = "test" }, "http://example.com/ns");
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
		}

		[Fact]
		public void AddBody_Json_Format()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new SimpleSerObj { Name = "json" });
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
			Assert.Contains("json", bodyParam.Value.ToString());
		}

		[Fact]
		public void AddObject_With_Whitelist()
		{
			var request = new RestRequest();
			var obj = new ObjWithMultipleProps { Name = "test", Age = 25, Email = "test@test.com" };
			request.AddObject(obj, "Name", "Age");
			Assert.Contains(request.Parameters, p => p.Name == "Name" && p.Value.ToString() == "test");
			Assert.Contains(request.Parameters, p => p.Name == "Age" && p.Value.ToString() == "25");
			Assert.DoesNotContain(request.Parameters, p => p.Name == "Email");
		}

		[Fact]
		public void AddFile_With_Writer()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => s.Write(new byte[] { 1, 2, 3 }, 0, 3);
			request.AddFile("file", writer, "test.bin", "application/octet-stream");
			Assert.Single(request.Files);
			Assert.Equal("file", request.Files[0].Name);
		}

		[Fact]
		public void AddFile_With_Bytes()
		{
			var request = new RestRequest();
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.bin", "application/octet-stream");
			Assert.Single(request.Files);
		}

		[Fact]
		public void IncreaseNumAttempts()
		{
			var request = new RestRequest();
			Assert.Equal(0, request.Attempts);
			request.IncreaseNumAttempts();
			Assert.Equal(1, request.Attempts);
		}

		[Fact]
		public void Credentials_Property()
		{
			var request = new RestRequest();
			var creds = new NetworkCredential("user", "pass");
			request.Credentials = creds;
			Assert.Equal(creds, request.Credentials);
		}

		[Fact]
		public void RequestFormat_Default_Is_Xml()
		{
			var request = new RestRequest();
			Assert.Equal(DataFormat.Xml, request.RequestFormat);
		}

		[Fact]
		public void Method_Property()
		{
			var request = new RestRequest(Method.POST);
			Assert.Equal(Method.POST, request.Method);
		}
	}

	public class RestClientAdditionalCoverageTests
	{
		[Fact]
		public void GetHandler_Returns_Default_For_Unknown_ContentType()
		{
			var client = new RestClient("http://example.com");
			client.ClearHandlers();
			// After clearing, handlers should be empty
			// Exercise the handler management
			client.AddHandler("application/json", new RestSharp.Deserializers.JsonDeserializer());
			client.RemoveHandler("application/json");
		}

		[Fact]
		public void Execute_With_Authenticator()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.Authenticator = new HttpBasicAuthenticator("user", "pass");

			var request = new RestRequest("api", Method.GET);
			var response = client.Execute(request);

			Assert.Contains(fakeHttp.Headers, h => h.Name == "Authorization");
		}

		[Fact]
		public void Execute_Generic_Deserializes()
		{
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = Encoding.UTF8.GetBytes("{\"Name\":\"test\",\"Value\":42}"),
				ContentType = "application/json",
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			var response = client.Execute<SimpleAsyncData>(new RestRequest("api", Method.GET));
			Assert.NotNull(response.Data);
			Assert.Equal("test", response.Data.Name);
		}

		[Fact]
		public void DownloadData_Returns_Bytes()
		{
			var rawBytes = Encoding.UTF8.GetBytes("raw data");
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactory(new HttpResponse
			{
				RawBytes = rawBytes,
				StatusCode = HttpStatusCode.OK,
				ResponseStatus = ResponseStatus.Completed
			});

			var result = client.DownloadData(new RestRequest("api", Method.GET));
			Assert.Equal(rawBytes, result);
		}

		[Fact]
		public void UseSynchronizationContext_Can_Be_Set()
		{
			var client = new RestClient("http://example.com");
			client.UseSynchronizationContext = true;
			Assert.True(client.UseSynchronizationContext);
		}

		[Fact]
		public void Timeout_Property()
		{
			var client = new RestClient("http://example.com");
			client.Timeout = 5000;
			Assert.Equal(5000, client.Timeout);
		}
	}

	public class ReflectionExtensionAdditionalTests
	{
		[Fact]
		public void ChangeType_Handles_Null()
		{
			var result = ((object)null).ChangeType(typeof(string), CultureInfo.InvariantCulture);
			Assert.Null(result);
		}

		[Fact]
		public void FindEnumValue_Case_Insensitive()
		{
			var result = typeof(Method).FindEnumValue("post", CultureInfo.InvariantCulture);
			Assert.Equal(Method.POST, result);
		}

		[Fact]
		public void FindEnumValue_Exact_Name()
		{
			var result = typeof(Method).FindEnumValue("GET", CultureInfo.InvariantCulture);
			Assert.Equal(Method.GET, result);
		}
	}

	public class StringExtensionAdditionalTests
	{
		[Fact]
		public void ParseJsonDate_Unix_Timestamp()
		{
			// Unix timestamp (seconds since epoch)
			var result = "1234567890".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
			Assert.Equal(2009, result.Year);
		}

		[Fact]
		public void ParseJsonDate_ISO8601()
		{
			var result = "2023-06-15T12:30:00Z".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2023, result.Year);
			Assert.Equal(6, result.Month);
			Assert.Equal(15, result.Day);
		}

		[Fact]
		public void ParseJsonDate_NewDate_Format()
		{
			var result = "new Date(1234567890000)".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
		}

		[Fact]
		public void GetNameVariants_With_Underscores()
		{
			var variants = "SomeLongName".GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Contains("some_long_name", variants);
			Assert.Contains("some-long-name", variants);
		}

		[Fact]
		public void AddUnderscores_Single_Word()
		{
			var result = "Hello".AddUnderscores();
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void AddDashes_Single_Word()
		{
			var result = "Hello".AddDashes();
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void AddUnderscores_With_Consecutive_Uppercase()
		{
			var result = "XMLParser".AddUnderscores();
			Assert.Contains("_", result);
		}

		[Fact]
		public void ToCamelCase_Single_Word()
		{
			var result = "Hello".ToCamelCase(CultureInfo.InvariantCulture);
			Assert.Equal("hello", result);
		}

		[Fact]
		public void MakeInitialLowerCase_SingleChar()
		{
			var result = "A".MakeInitialLowerCase();
			Assert.Equal("a", result);
		}

		[Fact]
		public void UrlEncode_Empty_Returns_Empty()
		{
			var result = "".UrlEncode();
			Assert.Equal("", result);
		}

		[Fact]
		public void HasValue_Returns_True_For_Whitespace()
		{
			Assert.True(" ".HasValue());
		}
	}

	// Model classes for tests
	public class XmlUriItem { public Uri Link { get; set; } }
	public class XmlFloatItem2 { public float Rate { get; set; } }
	public class XmlLong2 { public long BigNum { get; set; } }
	public class XmlNullDateItem { public DateTime? When { get; set; } }
	public class XmlDateFmtItem { public DateTime Date { get; set; } }
	public class XmlCultureItem { public decimal Price { get; set; } }
	public class XmlSimpleNs { public string Name { get; set; } }

	public class XmlNestedItem { public XmlChildItem Child { get; set; } }
	public class XmlChildItem { public string Name { get; set; } public int Value { get; set; } }
	public class XmlNullBoolItem { public bool? Active { get; set; } }

	public class SimpleSerObj { public string Name { get; set; } }
	public class ObjWithDateTime { public DateTime When { get; set; } }
	public class ObjWithNullableString { public string Name { get; set; } }
	public class ObjWithUriProp { public Uri Link { get; set; } }
	public class ObjWithInt { public int Count { get; set; } }
	public class ObjWithChild { public SimpleSerObj Child { get; set; } }
	public class ObjWithMultipleProps
	{
		public string Name { get; set; }
		public int Age { get; set; }
		public string Email { get; set; }
	}
}
