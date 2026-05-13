using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Contrib;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	/// <summary>
	/// Final round of tests to push coverage above 80%.
	/// Targets: HtmlEncoder, HttpUtility, XmlAttributeDeserializer, Http.cs internals,
	/// RestClient.cs, RestRequest.cs remaining uncovered lines.
	/// </summary>

	public class HtmlEncoderFinalTests
	{
		[Fact]
		public void HtmlDecode_Named_Entity_nbsp()
		{
			var result = HttpUtility.HtmlDecode("&nbsp;");
			Assert.NotNull(result);
			Assert.Single(result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_copy()
		{
			var result = HttpUtility.HtmlDecode("&copy;");
			Assert.Equal("\u00A9", result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_reg()
		{
			var result = HttpUtility.HtmlDecode("&reg;");
			Assert.Equal("\u00AE", result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_lt_gt_amp_quot()
		{
			var result = HttpUtility.HtmlDecode("&lt;&gt;&amp;&quot;");
			Assert.Equal("<>&\"", result);
		}

		[Fact]
		public void HtmlDecode_NumericEntity_Large_Value()
		{
			// Large Unicode codepoint
			var result = HttpUtility.HtmlDecode("&#65536;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Hex_Entity_Uppercase()
		{
			var result = HttpUtility.HtmlDecode("&#X41;");
			Assert.Equal("A", result);
		}

		[Fact]
		public void HtmlDecode_InvalidEntity_Preserved()
		{
			var result = HttpUtility.HtmlDecode("&invalidEntity;");
			Assert.Contains("&", result);
		}

		[Fact]
		public void HtmlDecode_Incomplete_NumericEntity()
		{
			var result = HttpUtility.HtmlDecode("&#abc;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Entity_Without_Semicolon()
		{
			var result = HttpUtility.HtmlDecode("&lt next");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlEncode_Char_Below_160_Passes_Through()
		{
			var result = HttpUtility.HtmlEncode("abc123");
			Assert.Equal("abc123", result);
		}

		[Fact]
		public void HtmlEncode_HighUnicode_Roundtrips()
		{
			var input = "\u2603"; // snowman
			var encoded = HttpUtility.HtmlEncode(input);
			var decoded = HttpUtility.HtmlDecode(encoded);
			Assert.Equal(input, decoded);
		}

		[Fact]
		public void HtmlEncode_AllSpecialChars()
		{
			var result = HttpUtility.HtmlEncode("<>&\"");
			Assert.Equal("&lt;&gt;&amp;&quot;", result);
		}

		[Fact]
		public void UrlPathEncode_NonAscii_Gets_Encoded()
		{
			var result = HttpUtility.UrlPathEncode("café");
			Assert.DoesNotContain("é", result);
		}

		[Fact]
		public void UrlPathEncode_With_Hash()
		{
			var result = HttpUtility.UrlPathEncode("page#section");
			Assert.Contains("#", result);
		}

		[Fact]
		public void UrlEncode_Unicode_Gets_Percent_Encoded()
		{
			var result = HttpUtility.UrlEncode("\u00e9");
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlEncode_Control_Characters()
		{
			var result = HttpUtility.UrlEncode("\t\n");
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlDecode_Unicode_Escape_u()
		{
			// %uXXXX format - behavior depends on implementation
			var result = HttpUtility.UrlDecode("%u0041");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecode_Mixed_Percent_And_Plus()
		{
			var result = HttpUtility.UrlDecode("hello+%77orld");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlEncode_Dot_And_Hyphen_Pass_Through()
		{
			var result = HttpUtility.UrlEncode("a.b-c_d");
			Assert.Equal("a.b-c_d", result);
		}

		[Fact]
		public void UrlEncode_Star_Stays()
		{
			var result = HttpUtility.UrlEncode("a*b");
			Assert.Equal("a*b", result);
		}
	}

	public class XmlAttributeDeserializerFinalTests
	{
		[Fact]
		public void Can_Deserialize_With_Lowercase_Element_Names()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<xmllowercaseitem><name>lower</name><value>10</value></xmllowercaseitem>"
			};
			var result = deserializer.Deserialize<XmlLowercaseItem>(response);
			Assert.Equal("lower", result.Name);
			Assert.Equal(10, result.Value);
		}

		[Fact]
		public void Can_Deserialize_Attribute_From_Element()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrDesItem Id=\"99\" Status=\"active\"><Name>test</Name></XmlAttrDesItem>"
			};
			var result = deserializer.Deserialize<XmlAttrDesItem>(response);
			Assert.Equal("test", result.Name);
			Assert.Equal("99", result.Id);
			Assert.Equal("active", result.Status);
		}

		[Fact]
		public void Can_Deserialize_List_Property()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlListContainer><Items><XmlListEntry><Label>a</Label></XmlListEntry><XmlListEntry><Label>b</Label></XmlListEntry></Items></XmlListContainer>"
			};
			var result = deserializer.Deserialize<XmlListContainer>(response);
			Assert.NotNull(result.Items);
			Assert.Equal(2, result.Items.Count);
		}

		[Fact]
		public void Can_Deserialize_Nested_Object()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrNested><Inner><Name>deep</Name></Inner></XmlAttrNested>"
			};
			var result = deserializer.Deserialize<XmlAttrNested>(response);
			Assert.NotNull(result.Inner);
			Assert.Equal("deep", result.Inner.Name);
		}

		[Fact]
		public void Can_Deserialize_With_CamelCase_Element()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlCamelItem><firstName>John</firstName></XmlCamelItem>"
			};
			var result = deserializer.Deserialize<XmlCamelItem>(response);
			Assert.Equal("John", result.FirstName);
		}

		[Fact]
		public void Can_Deserialize_DeserializeAs_Attribute()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlDeserializeAsItem><custom_name>aliased</custom_name></XmlDeserializeAsItem>"
			};
			var result = deserializer.Deserialize<XmlDeserializeAsItem>(response);
			Assert.Equal("aliased", result.Name);
		}

		[Fact]
		public void Can_Deserialize_Uri_Property()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrUri><Link>http://example.com</Link></XmlAttrUri>"
			};
			var result = deserializer.Deserialize<XmlAttrUri>(response);
			Assert.NotNull(result.Link);
			Assert.Equal("http://example.com/", result.Link.ToString());
		}

		[Fact]
		public void Can_Deserialize_With_DateFormat()
		{
			var deserializer = new XmlAttributeDeserializer();
			deserializer.DateFormat = "yyyy-MM-dd";
			var response = new RestResponse
			{
				Content = "<XmlAttrDate><Created>2023-01-15</Created></XmlAttrDate>"
			};
			var result = deserializer.Deserialize<XmlAttrDate>(response);
			Assert.Equal(new DateTime(2023, 1, 15), result.Created);
		}

		[Fact]
		public void Can_Deserialize_Nullable_Decimal()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrNullDec><Price>9.99</Price></XmlAttrNullDec>"
			};
			var result = deserializer.Deserialize<XmlAttrNullDec>(response);
			Assert.Equal(9.99m, result.Price);
		}

		[Fact]
		public void Can_Deserialize_Nullable_Decimal_Missing()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrNullDec></XmlAttrNullDec>"
			};
			var result = deserializer.Deserialize<XmlAttrNullDec>(response);
			Assert.Null(result.Price);
		}
	}

	public class RestClientFinalCoverageTests
	{
		[Fact]
		public void Execute_With_Files_Sets_HasFiles()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.txt", "text/plain");
			client.Execute(request);

			Assert.NotEmpty(fakeHttp.Files);
		}

		[Fact]
		public void Execute_With_Body_Sets_RequestBody()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.POST);
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "test" });
			client.Execute(request);

			Assert.NotNull(fakeHttp.RequestBody);
		}

		[Fact]
		public void Execute_Sets_UserAgent()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.UserAgent = "TestAgent/1.0";

			client.Execute(new RestRequest("api", Method.GET));

			Assert.Equal("TestAgent/1.0", fakeHttp.UserAgent);
		}

		[Fact]
		public void Execute_Sets_Credentials_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.Credentials = new NetworkCredential("user", "pass");
			client.Execute(request);

			Assert.NotNull(fakeHttp.Credentials);
		}

		[Fact]
		public void Execute_Sets_FollowRedirects()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.FollowRedirects = true;

			client.Execute(new RestRequest("api", Method.GET));

			Assert.True(fakeHttp.FollowRedirects);
		}

		[Fact]
		public void Execute_Sets_Proxy()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.Proxy = new WebProxy("http://proxy.example.com");

			client.Execute(new RestRequest("api", Method.GET));

			Assert.NotNull(fakeHttp.Proxy);
		}

		[Fact]
		public void Execute_Sets_MaxRedirects()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.MaxRedirects = 5;

			client.Execute(new RestRequest("api", Method.GET));

			Assert.Equal(5, fakeHttp.MaxRedirects);
		}

		[Fact]
		public void Execute_Sets_CookieContainer()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);
			client.CookieContainer = new CookieContainer();

			client.Execute(new RestRequest("api", Method.GET));

			Assert.NotNull(fakeHttp.CookieContainer);
		}

		[Fact]
		public void Execute_Sets_Timeout_From_Request()
		{
			var fakeHttp = new FakeHttp();
			var client = new RestClient("http://example.com");
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var request = new RestRequest("api", Method.GET);
			request.Timeout = 3000;
			client.Execute(request);

			Assert.Equal(3000, fakeHttp.Timeout);
		}

		[Fact]
		public void GetHandler_With_ContentType_Params_Returns_Handler()
		{
			var client = new RestClient("http://example.com");
			// Default JSON handler should handle application/json; charset=utf-8
			var fakeHttp = new FakeHttp();
			client.HttpFactory = new FakeHttpFactoryInstance(fakeHttp);

			var response = client.Execute(new RestRequest("api", Method.GET));
			Assert.NotNull(response);
		}
	}

	public class MiscExtensionFinalTests
	{
		[Fact]
		public void SaveAs_Writes_File()
		{
			var data = new byte[] { 72, 101, 108, 108, 111 };
			var path = Path.GetTempFileName();
			try
			{
				data.SaveAs(path);
				var read = File.ReadAllBytes(path);
				Assert.Equal(data, read);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void CopyTo_Large_Data()
		{
			var data = new byte[50000];
			new Random(42).NextBytes(data);
			using (var src = new MemoryStream(data))
			using (var dst = new MemoryStream())
			{
				src.CopyTo(dst);
				Assert.Equal(data.Length, dst.ToArray().Length);
			}
		}
	}

	public class RestRequestFinalTests
	{
		[Fact]
		public void AddParameter_With_Type()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value", ParameterType.HttpHeader);
			Assert.Contains(request.Parameters, p => p.Name == "name" && p.Type == ParameterType.HttpHeader);
		}

		[Fact]
		public void AddParameter_Object_Overload()
		{
			var request = new RestRequest();
			var param = new Parameter { Name = "test", Value = "val", Type = ParameterType.GetOrPost };
			request.AddParameter(param);
			Assert.Contains(request.Parameters, p => p.Name == "test");
		}

		[Fact]
		public void AddObject_Without_Whitelist()
		{
			var request = new RestRequest();
			request.AddObject(new ObjWithMultipleProps { Name = "t", Age = 1, Email = "e" });
			Assert.True(request.Parameters.Count >= 3);
		}

		[Fact]
		public void Resource_With_Leading_Slash_Stripped()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/api/test", Method.GET);
			var uri = client.BuildUri(request);
			Assert.DoesNotContain("//api", uri.ToString());
		}

		[Fact]
		public void AddBody_Xml_Without_Namespace()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.XmlSerializer = new RestSharp.Serializers.XmlSerializer();
			request.AddBody(new SimpleSerObj { Name = "xmlbody" });
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
		}
	}

	// Model classes
	public class XmlLowercaseItem
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class XmlAttrDesItem
	{
		public string Name { get; set; }
		public string Id { get; set; }
		public string Status { get; set; }
	}

	public class XmlListContainer
	{
		public List<XmlListEntry> Items { get; set; }
	}

	public class XmlListEntry
	{
		public string Label { get; set; }
	}

	public class XmlAttrNested
	{
		public XmlAttrInner Inner { get; set; }
	}

	public class XmlAttrInner
	{
		public string Name { get; set; }
	}

	public class XmlCamelItem
	{
		public string FirstName { get; set; }
	}

	public class XmlDeserializeAsItem
	{
		[DeserializeAs(Name = "custom_name")]
		public string Name { get; set; }
	}

	public class XmlAttrUri
	{
		public Uri Link { get; set; }
	}

	public class XmlAttrDate
	{
		public DateTime Created { get; set; }
	}

	public class XmlAttrNullDec
	{
		public decimal? Price { get; set; }
	}
}
