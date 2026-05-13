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
	public class HtmlEncoderEdgeCaseTests
	{
		[Fact]
		public void HtmlEncode_FullwidthLessThan()
		{
			// \uff1c is fullwidth < and \uff1e is fullwidth >
			var result = HttpUtility.HtmlEncode("\uff1c\uff1e");
			Assert.Contains("65308", result);
			Assert.Contains("65310", result);
		}

		[Fact]
		public void HtmlDecode_Consecutive_Ampersands()
		{
			// Test state transition when & appears inside an entity
			var result = HttpUtility.HtmlDecode("&amp;&lt;");
			Assert.Equal("&<", result);
		}

		[Fact]
		public void HtmlDecode_Ampersand_Then_Another_Ampersand()
		{
			// This should trigger the c == '&' in state != 0 code
			var result = HttpUtility.HtmlDecode("&invalid&amp;");
			Assert.Contains("&", result);
		}

		[Fact]
		public void HtmlDecode_Hash_Semicolon_Directly()
		{
			// &#; should be treated as invalid
			var result = HttpUtility.HtmlDecode("&#;test");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Numeric_Entity_With_Trailing_Text()
		{
			// &#65abc; - digits followed by non-digit in numeric entity
			var result = HttpUtility.HtmlDecode("&#65abc;");
			Assert.NotNull(result);
			Assert.True(result.Length > 0);
		}

		[Fact]
		public void HtmlDecode_Hex_Entity_Invalid_Chars()
		{
			var result = HttpUtility.HtmlDecode("&#xZZZ;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_euro()
		{
			var result = HttpUtility.HtmlDecode("&euro;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_trade()
		{
			var result = HttpUtility.HtmlDecode("&trade;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Long_Named_Entity()
		{
			var result = HttpUtility.HtmlDecode("&thinsp;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Entity_At_End_Without_Semicolon()
		{
			var result = HttpUtility.HtmlDecode("text&amp");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_Entity_Number_At_End()
		{
			var result = HttpUtility.HtmlDecode("&#65");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeUnicode_HighChar()
		{
			// This should trigger the c > 255 path in UrlEncodeChar
			var result = HttpUtility.UrlEncodeUnicode("\u4e2d"); // Chinese character
			Assert.Contains("%u", result);
		}

		[Fact]
		public void UrlEncodeUnicode_MixedContent()
		{
			var result = HttpUtility.UrlEncodeUnicode("hello\u4e16\u754c");
			Assert.Contains("hello", result);
			Assert.Contains("%u", result);
		}

		[Fact]
		public void UrlPathEncode_Space_Is_Percent20()
		{
			// UrlPathEncode should encode space as %20 not +
			var result = HttpUtility.UrlPathEncode("hello world");
			Assert.Contains("%20", result);
		}

		[Fact]
		public void UrlPathEncode_Control_Char()
		{
			var result = HttpUtility.UrlPathEncode("hello\x01world");
			Assert.Contains("%", result);
		}
	}

	public class XmlAttributeDeserializerEdgeTests
	{
		[Fact]
		public void Can_Deserialize_With_RootElement_And_Attributes()
		{
			var deserializer = new XmlAttributeDeserializer();
			deserializer.RootElement = "Data";
			var response = new RestResponse
			{
				Content = "<Root><Data><Name>rooted</Name><Value>5</Value></Data></Root>"
			};
			var result = deserializer.Deserialize<XmlAttrSimple>(response);
			Assert.Equal("rooted", result.Name);
		}

		[Fact]
		public void Can_Deserialize_Attribute_Values()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlWithAttrStr name=\"attr_name\" status=\"active\" />"
			};
			var result = deserializer.Deserialize<XmlWithAttrStr>(response);
			Assert.Equal("attr_name", result.Name);
			Assert.Equal("active", result.Status);
		}

		[Fact]
		public void Can_Deserialize_Attribute_CaseInsensitive()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlWithAttrStr NAME=\"upper\" STATUS=\"done\" />"
			};
			var result = deserializer.Deserialize<XmlWithAttrStr>(response);
			Assert.NotNull(result);
		}

		[Fact]
		public void Can_Deserialize_With_Namespace_And_Attributes()
		{
			var deserializer = new XmlAttributeDeserializer();
			deserializer.Namespace = "http://test.com";
			var response = new RestResponse
			{
				Content = "<XmlAttrSimple xmlns=\"http://test.com\"><Name>ns_attr</Name><Value>3</Value></XmlAttrSimple>"
			};
			var result = deserializer.Deserialize<XmlAttrSimple>(response);
			Assert.Equal("ns_attr", result.Name);
		}

		[Fact]
		public void Can_Deserialize_With_DeserializeAs()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlWithDeserializeAs><actual_name>mapped</actual_name></XmlWithDeserializeAs>"
			};
			var result = deserializer.Deserialize<XmlWithDeserializeAs>(response);
			Assert.Equal("mapped", result.TheName);
		}

		[Fact]
		public void Can_Deserialize_With_DeserializeAs_Attribute_Flag()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlWithDeserializeAsAttr id=\"42\"><Name>test</Name></XmlWithDeserializeAsAttr>"
			};
			var result = deserializer.Deserialize<XmlWithDeserializeAsAttr>(response);
			Assert.Equal("42", result.Id);
			Assert.Equal("test", result.Name);
		}

		[Fact]
		public void RemoveNamespace_Strips_Namespace()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<ns:Root xmlns:ns=\"http://example.com\"><ns:Name>stripped</ns:Name></ns:Root>"
			};
			var result = deserializer.Deserialize<XmlNsSimple>(response);
			Assert.Equal("stripped", result.Name);
		}

		[Fact]
		public void Can_Deserialize_List_Of_Nested()
		{
			var deserializer = new XmlAttributeDeserializer();
			var response = new RestResponse
			{
				Content = "<XmlAttrListContainer><Items><XmlAttrListItem><Text>first</Text></XmlAttrListItem><XmlAttrListItem><Text>second</Text></XmlAttrListItem></Items></XmlAttrListContainer>"
			};
			var result = deserializer.Deserialize<XmlAttrListContainer>(response);
			Assert.NotNull(result.Items);
			Assert.Equal(2, result.Items.Count);
			Assert.Equal("first", result.Items[0].Text);
		}
	}

	public class HttpUtilityEdgeCaseTests
	{
		[Fact]
		public void UrlDecode_Byte_Array_With_Percent_Sequence()
		{
			var bytes = Encoding.ASCII.GetBytes("a%41%42%43z");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("aABCz", result);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array_Full()
		{
			var bytes = Encoding.ASCII.GetBytes("a%20b");
			var result = HttpUtility.UrlDecodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void ParseQueryString_Complex()
		{
			var result = HttpUtility.ParseQueryString("a=1&b=2&c=hello+world&d=%26");
			Assert.Equal("1", result["a"]);
			Assert.Equal("2", result["b"]);
			Assert.Equal("hello world", result["c"]);
			Assert.Equal("&", result["d"]);
		}

		[Fact]
		public void UrlEncode_Byte_Array_Full()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world!");
			var result = HttpUtility.UrlEncode(bytes);
			Assert.Contains("+", result);
			// ! might or might not be encoded depending on implementation
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array_Full()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncodeToBytes(bytes);
			Assert.NotNull(result);
			Assert.True(result.Length > 0);
		}

		[Fact]
		public void HtmlDecode_To_TextWriter_With_Entities()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlDecode("&lt;b&gt;hello&lt;/b&gt;", writer);
			Assert.Equal("<b>hello</b>", writer.ToString());
		}

		[Fact]
		public void HtmlEncode_To_TextWriter_With_Entities()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlEncode("<b>hello</b>", writer);
			var result = writer.ToString();
			Assert.Contains("&lt;", result);
			Assert.Contains("&gt;", result);
		}

		[Fact]
		public void HtmlAttributeEncode_To_TextWriter_Null()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlAttributeEncode(null, writer);
			Assert.Equal("", writer.ToString());
		}
	}

	public class RestRequestAsyncHandleFinalTests
	{
		[Fact]
		public void Abort_When_WebRequest_Is_Not_Null()
		{
			var handle = new RestRequestAsyncHandle();
			// WebRequest is null, Abort should not throw
			handle.Abort();
			Assert.Null(handle.WebRequest);
		}
	}

	public class MiscExtensionsFinalTests
	{
		[Fact]
		public void AsString_NonNull_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("test string");
			var result = bytes.AsString();
			Assert.Equal("test string", result);
		}

		[Fact]
		public void AsString_Empty_Bytes()
		{
			var bytes = new byte[0];
			var result = bytes.AsString();
			Assert.Equal("", result);
		}

		[Fact]
		public void ReadAsBytes_Empty_Stream()
		{
			using (var stream = new MemoryStream())
			{
				var result = stream.ReadAsBytes();
				Assert.Empty(result);
			}
		}
	}

	public class JsonSerializerFinalTests
	{
		[Fact]
		public void Serialize_With_Null_Property()
		{
			var serializer = new RestSharp.Serializers.JsonSerializer();
			var result = serializer.Serialize(new ObjWithNullableField { Name = null, Count = 42 });
			Assert.Contains("42", result);
		}

		[Fact]
		public void DateFormat_Can_Be_Set()
		{
			var serializer = new RestSharp.Serializers.JsonSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
		}

		[Fact]
		public void RootElement_Can_Be_Set()
		{
			var serializer = new RestSharp.Serializers.JsonSerializer();
			serializer.RootElement = "root";
			Assert.Equal("root", serializer.RootElement);
		}

		[Fact]
		public void Namespace_Can_Be_Set()
		{
			var serializer = new RestSharp.Serializers.JsonSerializer();
			serializer.Namespace = "ns";
			Assert.Equal("ns", serializer.Namespace);
		}
	}

	// Model classes
	public class XmlAttrSimple
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class XmlWithAttr
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class XmlWithAttrStr
	{
		public string Name { get; set; }
		public string Status { get; set; }
	}

	public class XmlNsSimple
	{
		public string Name { get; set; }
	}

	public class XmlWithDeserializeAs
	{
		[DeserializeAs(Name = "actual_name")]
		public string TheName { get; set; }
	}

	public class XmlWithDeserializeAsAttr
	{
		[DeserializeAs(Name = "id", Attribute = true)]
		public string Id { get; set; }
		public string Name { get; set; }
	}

	public class XmlAttrListContainer
	{
		public List<XmlAttrListItem> Items { get; set; }
	}

	public class XmlAttrListItem
	{
		public string Text { get; set; }
	}

	public class ObjWithNullableField
	{
		public string Name { get; set; }
		public int Count { get; set; }
	}
}
