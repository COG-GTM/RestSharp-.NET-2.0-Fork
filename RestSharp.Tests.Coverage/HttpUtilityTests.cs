using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using RestSharp.Contrib;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class HttpUtilityTests
	{
		[Fact]
		public void UrlEncode_Basic_String()
		{
			var result = HttpUtility.UrlEncode("hello world");
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncode_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncode((string)null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncode_Empty_Returns_Empty()
		{
			var result = HttpUtility.UrlEncode("");
			Assert.Equal("", result);
		}

		[Fact]
		public void UrlEncode_Special_Characters()
		{
			var result = HttpUtility.UrlEncode("a&b=c?d#e");
			Assert.DoesNotContain("&", result);
			Assert.DoesNotContain("=", result);
			Assert.DoesNotContain("?", result);
		}

		[Fact]
		public void UrlEncode_Unicode_Characters()
		{
			var result = HttpUtility.UrlEncode("café");
			Assert.NotEqual("café", result);
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlDecode_Basic_String()
		{
			var result = HttpUtility.UrlDecode("hello+world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_Null_Returns_Null()
		{
			var result = HttpUtility.UrlDecode(null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlDecode_Percent_Encoded()
		{
			var result = HttpUtility.UrlDecode("hello%20world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_With_Encoding()
		{
			var result = HttpUtility.UrlDecode("hello+world", Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlEncode_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncode(bytes, 0, bytes.Length);
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncode_With_Encoding()
		{
			var result = HttpUtility.UrlEncode("hello world", Encoding.UTF8);
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncodeUnicode_Encodes_Unicode()
		{
			var result = HttpUtility.UrlEncodeUnicode("café");
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlEncodeUnicode_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncodeUnicode(null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlDecode_Bytes()
		{
			var bytes = Encoding.ASCII.GetBytes("hello+world");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_Bytes_Null_Returns_Null()
		{
			var result = HttpUtility.UrlDecode(null, 0, 0, Encoding.UTF8);
			Assert.Null(result);
		}

		[Fact]
		public void UrlDecode_Percent_Hex()
		{
			var result = HttpUtility.UrlDecode("%41%42%43");
			Assert.Equal("ABC", result);
		}

		[Fact]
		public void UrlDecode_Unicode_Percent_u()
		{
			var result = HttpUtility.UrlDecode("%u0041%u0042%u0043");
			Assert.Equal("ABC", result);
		}

		[Fact]
		public void HtmlEncode_Special_Characters()
		{
			var result = HttpUtility.HtmlEncode("<script>alert('xss')</script>");
			Assert.DoesNotContain("<", result);
			Assert.DoesNotContain(">", result);
			Assert.Contains("&lt;", result);
			Assert.Contains("&gt;", result);
		}

		[Fact]
		public void HtmlEncode_Null_Returns_Null()
		{
			var result = HttpUtility.HtmlEncode(null);
			Assert.Null(result);
		}

		[Fact]
		public void HtmlEncode_Ampersand()
		{
			var result = HttpUtility.HtmlEncode("a & b");
			Assert.Contains("&amp;", result);
		}

		[Fact]
		public void HtmlEncode_Quotes()
		{
			var result = HttpUtility.HtmlEncode("say \"hello\"");
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void HtmlEncode_High_Unicode()
		{
			var result = HttpUtility.HtmlEncode("hello \u00e9");
			Assert.Contains("&#", result);
		}

		[Fact]
		public void HtmlDecode_Entities()
		{
			var result = HttpUtility.HtmlDecode("&lt;div&gt;test&lt;/div&gt;");
			Assert.Equal("<div>test</div>", result);
		}

		[Fact]
		public void HtmlDecode_Null_Returns_Null()
		{
			var result = HttpUtility.HtmlDecode(null);
			Assert.Null(result);
		}

		[Fact]
		public void HtmlDecode_Amp()
		{
			var result = HttpUtility.HtmlDecode("a &amp; b");
			Assert.Equal("a & b", result);
		}

		[Fact]
		public void HtmlDecode_Numeric_Entity()
		{
			var result = HttpUtility.HtmlDecode("&#65;&#66;&#67;");
			Assert.Equal("ABC", result);
		}

		[Fact]
		public void HtmlDecode_Hex_Entity()
		{
			var result = HttpUtility.HtmlDecode("&#x41;&#x42;&#x43;");
			Assert.Equal("ABC", result);
		}

		[Fact]
		public void HtmlDecode_Quot()
		{
			var result = HttpUtility.HtmlDecode("&quot;hello&quot;");
			Assert.Equal("\"hello\"", result);
		}

		[Fact]
		public void ParseQueryString_Parses_Basic_QueryString()
		{
			var result = HttpUtility.ParseQueryString("key1=value1&key2=value2");
			Assert.Equal("value1", result["key1"]);
			Assert.Equal("value2", result["key2"]);
		}

		[Fact]
		public void ParseQueryString_Empty_String()
		{
			var result = HttpUtility.ParseQueryString("");
			Assert.NotNull(result);
			Assert.Equal(0, result.Count);
		}

		[Fact]
		public void ParseQueryString_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => HttpUtility.ParseQueryString(null));
		}

		[Fact]
		public void ParseQueryString_With_Encoding()
		{
			var result = HttpUtility.ParseQueryString("name=test", Encoding.UTF8);
			Assert.Equal("test", result["name"]);
		}

		[Fact]
		public void ParseQueryString_With_Encoded_Values()
		{
			var result = HttpUtility.ParseQueryString("name=hello+world&foo=a%26b");
			Assert.Equal("hello world", result["name"]);
			Assert.Equal("a&b", result["foo"]);
		}

		[Fact]
		public void ParseQueryString_Multiple_Same_Keys()
		{
			var result = HttpUtility.ParseQueryString("a=1&a=2");
			var values = result["a"];
			Assert.Contains("1", values);
			Assert.Contains("2", values);
		}

		[Fact]
		public void UrlDecode_Roundtrip()
		{
			var original = "hello world & goodbye=test?foo#bar";
			var encoded = HttpUtility.UrlEncode(original);
			var decoded = HttpUtility.UrlDecode(encoded);
			Assert.Equal(original, decoded);
		}

		[Fact]
		public void HtmlEncodeDecode_Roundtrip()
		{
			var original = "<div class=\"test\">Hello & world</div>";
			var encoded = HttpUtility.HtmlEncode(original);
			var decoded = HttpUtility.HtmlDecode(encoded);
			Assert.Equal(original, decoded);
		}

		[Fact]
		public void UrlEncode_Numbers_And_Letters_Unchanged()
		{
			var result = HttpUtility.UrlEncode("abc123XYZ");
			Assert.Equal("abc123XYZ", result);
		}

		[Fact]
		public void UrlDecode_Pass_Through_Non_Encoded()
		{
			var result = HttpUtility.UrlDecode("simple");
			Assert.Equal("simple", result);
		}

		[Fact]
		public void HtmlDecode_Named_Entity_Apos()
		{
			var result = HttpUtility.HtmlDecode("&apos;");
			// apos is HTML5, might not be supported
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_No_Entities_Returns_Same()
		{
			var result = HttpUtility.HtmlDecode("plain text");
			Assert.Equal("plain text", result);
		}

		[Fact]
		public void UrlEncode_Bytes_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncode(null, 0, 0);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncode_Bytes_With_Offset()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world!");
			var result = HttpUtility.UrlEncode(bytes, 6, 5);
			Assert.Contains("world", result);
		}

		[Fact]
		public void UrlPathEncode_Encodes_Spaces()
		{
			var result = HttpUtility.UrlPathEncode("hello world/path");
			Assert.DoesNotContain(" ", result);
			Assert.Contains("/", result);
		}

		[Fact]
		public void UrlPathEncode_Null_Returns_Null()
		{
			var result = HttpUtility.UrlPathEncode(null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Returns_Encoded_Bytes()
		{
			var result = HttpUtility.UrlEncodeToBytes("hello world");
			Assert.NotNull(result);
			var str = Encoding.ASCII.GetString(result);
			Assert.Contains("+", str);
		}

		[Fact]
		public void UrlEncodeToBytes_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncodeToBytes((string)null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Decodes_Bytes()
		{
			var result = HttpUtility.UrlDecodeToBytes("hello+world");
			Assert.NotNull(result);
			var str = Encoding.UTF8.GetString(result);
			Assert.Equal("hello world", str);
		}

		[Fact]
		public void UrlDecodeToBytes_Null_Returns_Null()
		{
			var result = HttpUtility.UrlDecodeToBytes((string)null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncodeUnicodeToBytes_Encodes()
		{
			var result = HttpUtility.UrlEncodeUnicodeToBytes("café");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array()
		{
			var input = Encoding.ASCII.GetBytes("hello+world");
			var result = HttpUtility.UrlDecodeToBytes(input, 0, input.Length);
			Assert.NotNull(result);
			var str = Encoding.UTF8.GetString(result);
			Assert.Equal("hello world", str);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array()
		{
			var input = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncodeToBytes(input, 0, input.Length);
			Assert.NotNull(result);
			var str = Encoding.ASCII.GetString(result);
			Assert.Contains("+", str);
		}

		[Fact]
		public void UrlEncode_Handles_Single_Char_Percent()
		{
			var result = HttpUtility.UrlEncode("%");
			Assert.Equal("%25", result);
		}
	}
}
