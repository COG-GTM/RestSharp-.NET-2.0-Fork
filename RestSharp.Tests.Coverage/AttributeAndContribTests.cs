using System;
using System.Globalization;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using RestSharp.Contrib;
using Xunit;

namespace RestSharp.Tests
{
	public class AttributeAndContribTests
	{
		// DeserializeAsAttribute
		[Fact]
		public void DeserializeAsAttribute_Name()
		{
			var attr = new DeserializeAsAttribute { Name = "custom_name" };
			Assert.Equal("custom_name", attr.Name);
		}

		[Fact]
		public void DeserializeAsAttribute_Attribute()
		{
			var attr = new DeserializeAsAttribute { Attribute = true };
			Assert.True(attr.Attribute);
		}

		// SerializeAsAttribute
		[Fact]
		public void SerializeAsAttribute_Defaults()
		{
			var attr = new SerializeAsAttribute();
			Assert.Equal(NameStyle.AsIs, attr.NameStyle);
			Assert.Equal(int.MaxValue, attr.Index);
			Assert.Equal(CultureInfo.InvariantCulture, attr.Culture);
		}

		[Fact]
		public void SerializeAsAttribute_TransformName_AsIs()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.AsIs };
			Assert.Equal("TestInput", attr.TransformName("TestInput"));
		}

		[Fact]
		public void SerializeAsAttribute_TransformName_CamelCase()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.CamelCase };
			Assert.Equal("testInput", attr.TransformName("TestInput"));
		}

		[Fact]
		public void SerializeAsAttribute_TransformName_LowerCase()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.LowerCase };
			Assert.Equal("testinput", attr.TransformName("TestInput"));
		}

		[Fact]
		public void SerializeAsAttribute_TransformName_PascalCase()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.PascalCase };
			Assert.Equal("TestInput", attr.TransformName("test_input"));
		}

		[Fact]
		public void SerializeAsAttribute_TransformName_With_CustomName()
		{
			var attr = new SerializeAsAttribute { Name = "CustomName", NameStyle = NameStyle.LowerCase };
			Assert.Equal("customname", attr.TransformName("OriginalInput"));
		}

		[Fact]
		public void SerializeAsAttribute_All_Properties()
		{
			var attr = new SerializeAsAttribute
			{
				Name = "myProp",
				Attribute = true,
				Culture = CultureInfo.CurrentCulture,
				NameStyle = NameStyle.CamelCase,
				Index = 5
			};
			Assert.Equal("myProp", attr.Name);
			Assert.True(attr.Attribute);
			Assert.Equal(5, attr.Index);
		}

		// NameStyle enum values
		[Fact]
		public void NameStyle_Enum_Values()
		{
			Assert.Equal(NameStyle.AsIs, (NameStyle)0);
			Assert.Equal(NameStyle.CamelCase, (NameStyle)1);
			Assert.Equal(NameStyle.LowerCase, (NameStyle)2);
			Assert.Equal(NameStyle.PascalCase, (NameStyle)3);
		}

		// HttpUtility tests
		[Fact]
		public void HttpUtility_HtmlEncode_Basic()
		{
			Assert.Equal("&lt;b&gt;", HttpUtility.HtmlEncode("<b>"));
		}

		[Fact]
		public void HttpUtility_HtmlEncode_Ampersand()
		{
			Assert.Equal("a &amp; b", HttpUtility.HtmlEncode("a & b"));
		}

		[Fact]
		public void HttpUtility_HtmlEncode_Quotes()
		{
			Assert.Equal("&quot;test&quot;", HttpUtility.HtmlEncode("\"test\""));
		}

		[Fact]
		public void HttpUtility_HtmlDecode_Basic()
		{
			Assert.Equal("<b>", HttpUtility.HtmlDecode("&lt;b&gt;"));
		}

		[Fact]
		public void HttpUtility_HtmlDecode_Numeric()
		{
			Assert.Equal("A", HttpUtility.HtmlDecode("&#65;"));
		}

		[Fact]
		public void HttpUtility_HtmlEncode_Null()
		{
			Assert.Null(HttpUtility.HtmlEncode((string)null));
		}

		[Fact]
		public void HttpUtility_HtmlDecode_Null()
		{
			Assert.Null(HttpUtility.HtmlDecode(null));
		}

		[Fact]
		public void HttpUtility_UrlEncode_Basic()
		{
			var result = HttpUtility.UrlEncode("hello world");
			Assert.Equal("hello+world", result);
		}

		[Fact]
		public void HttpUtility_UrlDecode_Basic()
		{
			var result = HttpUtility.UrlDecode("hello+world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void HttpUtility_UrlEncode_Null()
		{
			Assert.Null(HttpUtility.UrlEncode((string)null));
		}

		[Fact]
		public void HttpUtility_UrlDecode_Null()
		{
			Assert.Null(HttpUtility.UrlDecode((string)null));
		}

		[Fact]
		public void HttpUtility_UrlEncode_SpecialChars()
		{
			var result = HttpUtility.UrlEncode("a=1&b=2");
			Assert.Contains("%3d", result, StringComparison.OrdinalIgnoreCase);
			Assert.Contains("%26", result, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void HttpUtility_UrlDecode_Percent()
		{
			var result = HttpUtility.UrlDecode("hello%20world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void HttpUtility_ParseQueryString()
		{
			var result = HttpUtility.ParseQueryString("a=1&b=2");
			Assert.Equal("1", result["a"]);
			Assert.Equal("2", result["b"]);
		}

		[Fact]
		public void HttpUtility_ParseQueryString_Empty()
		{
			var result = HttpUtility.ParseQueryString("");
			Assert.Equal(0, result.Count);
		}

		[Fact]
		public void HttpUtility_ParseQueryString_With_QuestionMark()
		{
			var result = HttpUtility.ParseQueryString("?a=1");
			Assert.Equal("1", result["a"]);
		}
	}
}
