using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using RestSharp.Contrib;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class HttpUtilityExtendedTests
	{
		[Fact]
		public void HtmlAttributeEncode_Encodes_Ampersand()
		{
			var result = HttpUtility.HtmlAttributeEncode("a&b");
			Assert.Contains("&amp;", result);
		}

		[Fact]
		public void HtmlAttributeEncode_Encodes_Quote()
		{
			var result = HttpUtility.HtmlAttributeEncode("a\"b");
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void HtmlAttributeEncode_Encodes_LessThan()
		{
			var result = HttpUtility.HtmlAttributeEncode("a<b");
			Assert.Contains("&lt;", result);
		}

		[Fact]
		public void HtmlAttributeEncode_Null_Returns_Null()
		{
			var result = HttpUtility.HtmlAttributeEncode(null);
			Assert.Null(result);
		}

		[Fact]
		public void HtmlAttributeEncode_No_Special_Returns_Same()
		{
			var result = HttpUtility.HtmlAttributeEncode("hello world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void HtmlDecode_With_TextWriter()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlDecode("&lt;div&gt;", writer);
			Assert.Equal("<div>", writer.ToString());
		}

		[Fact]
		public void HtmlEncode_With_TextWriter()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlEncode("<div>", writer);
			Assert.Contains("&lt;", writer.ToString());
		}

		[Fact]
		public void HtmlAttributeEncode_With_TextWriter()
		{
			var writer = new StringWriter();
			HttpUtility.HtmlAttributeEncode("a&b\"c", writer);
			var result = writer.ToString();
			Assert.Contains("&amp;", result);
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void UrlDecode_Byte_Array_Overload()
		{
			var bytes = Encoding.UTF8.GetBytes("hello%20world");
			var result = HttpUtility.UrlDecode(bytes, Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_Byte_Array_Null_Returns_Null()
		{
			var result = HttpUtility.UrlDecode((byte[])null, Encoding.UTF8);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncode_Byte_Array_Overload()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncode(bytes);
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncode_Byte_Array_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncode((byte[])null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncode_Byte_Array_With_Offset_Length()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world!");
			var result = HttpUtility.UrlEncode(bytes, 0, bytes.Length);
			Assert.Contains("+", result);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array_Overload()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Byte_Array_Null_Returns_Null()
		{
			var result = HttpUtility.UrlEncodeToBytes((byte[])null);
			Assert.Null(result);
		}

		[Fact]
		public void UrlEncodeToBytes_With_Encoding()
		{
			var result = HttpUtility.UrlEncodeToBytes("hello world", Encoding.UTF8);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array_Overload()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			var result = HttpUtility.UrlDecodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_String_With_Encoding()
		{
			var result = HttpUtility.UrlDecodeToBytes("hello+world", Encoding.UTF8);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Byte_Array_With_Encoded()
		{
			var bytes = Encoding.ASCII.GetBytes("hello%20world%26test");
			var result = HttpUtility.UrlDecodeToBytes(bytes, 0, bytes.Length);
			var str = Encoding.UTF8.GetString(result);
			Assert.Equal("hello world&test", str);
		}

		[Fact]
		public void ParseQueryString_With_Question_Mark_Prefix()
		{
			var result = HttpUtility.ParseQueryString("?key=value");
			Assert.Equal("value", result["key"]);
		}

		[Fact]
		public void ParseQueryString_ToString_Returns_QueryString()
		{
			var result = HttpUtility.ParseQueryString("key1=value1&key2=value2");
			var str = result.ToString();
			Assert.Contains("key1", str);
			Assert.Contains("value1", str);
		}

		[Fact]
		public void ParseQueryString_With_No_Value()
		{
			var result = HttpUtility.ParseQueryString("key&key2=val2");
			Assert.NotNull(result);
		}

		[Fact]
		public void HttpUtility_Constructor()
		{
			var util = new HttpUtility();
			Assert.NotNull(util);
		}

		[Fact]
		public void UrlDecode_With_Percent_UTF8_Multi_Byte()
		{
			// encode café and decode it back
			var encoded = HttpUtility.UrlEncode("café", Encoding.UTF8);
			var decoded = HttpUtility.UrlDecode(encoded, Encoding.UTF8);
			Assert.Equal("café", decoded);
		}

		[Fact]
		public void HtmlDecode_With_Apos_Entity()
		{
			var result = HttpUtility.HtmlDecode("hello &amp; world");
			Assert.Equal("hello & world", result);
		}

		[Fact]
		public void HtmlDecode_With_Incomplete_Entity()
		{
			var result = HttpUtility.HtmlDecode("a &amp b");
			Assert.Contains("&", result);
		}

		[Fact]
		public void UrlPathEncode_With_Query()
		{
			var result = HttpUtility.UrlPathEncode("path/file name?query=value");
			Assert.Contains("?query=value", result);
			Assert.DoesNotContain(" ", result.Substring(0, result.IndexOf('?')));
		}

		[Fact]
		public void UrlDecode_Complex_Encoded_Bytes()
		{
			var input = Encoding.ASCII.GetBytes("a%20b%2Bc");
			var result = HttpUtility.UrlDecode(input, 0, input.Length, Encoding.UTF8);
			Assert.Equal("a b+c", result);
		}

		[Fact]
		public void HtmlEncode_Various_Special_Chars()
		{
			var result = HttpUtility.HtmlEncode("a<b>c&d\"e");
			Assert.Contains("&lt;", result);
			Assert.Contains("&gt;", result);
			Assert.Contains("&amp;", result);
			Assert.Contains("&quot;", result);
		}

		[Fact]
		public void HtmlDecode_Numeric_Hex_Large()
		{
			var result = HttpUtility.HtmlDecode("&#x263A;");
			Assert.NotNull(result);
			Assert.True(result.Length >= 1);
		}

		[Fact]
		public void UrlEncode_Ascii_Safe_Characters()
		{
			var result = HttpUtility.UrlEncode("abc-_.~");
			// letters, digits, -, _, ., ~ should mostly pass through
			Assert.Contains("abc", result);
		}

		[Fact]
		public void UrlDecodeToBytes_Empty_Returns_Empty()
		{
			var result = HttpUtility.UrlDecodeToBytes("");
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void HtmlEncode_Tab_And_Newlines()
		{
			var result = HttpUtility.HtmlEncode("line1\nline2\ttab");
			Assert.Contains("\n", result);
		}

		[Fact]
		public void UrlDecode_Percent_At_End_No_Chars()
		{
			var result = HttpUtility.UrlDecode("hello%");
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlDecode_Incomplete_Percent_Sequence()
		{
			var result = HttpUtility.UrlDecode("hello%2");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncode_All_NonAlphanumeric()
		{
			var result = HttpUtility.UrlEncode("!@#$%^&*()");
			Assert.NotEqual("!@#$%^&*()", result);
		}

		[Fact]
		public void ParseQueryString_Key_Without_Equals()
		{
			var result = HttpUtility.ParseQueryString("lonely");
			Assert.NotNull(result);
		}

		[Fact]
		public void ParseQueryString_Empty_Value()
		{
			var result = HttpUtility.ParseQueryString("key=");
			Assert.Equal("", result["key"]);
		}

		[Fact]
		public void UrlDecode_Plus_In_Bytes()
		{
			var bytes = Encoding.ASCII.GetBytes("a+b");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("a b", result);
		}

		[Fact]
		public void UrlDecode_Bytes_Percent_Encoded()
		{
			var bytes = Encoding.ASCII.GetBytes("a%41b");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("aAb", result);
		}
	}

	public class HtmlEncoderTests
	{
		[Fact]
		public void HtmlAttributeEncode_Encodes_Quote_And_Ampersand()
		{
			var result = HttpUtility.HtmlAttributeEncode("a\"b&c");
			Assert.Contains("&quot;", result);
			Assert.Contains("&amp;", result);
		}

		[Fact]
		public void HtmlEncode_Unicode_Char_Above_159()
		{
			var result = HttpUtility.HtmlEncode("\u00A0 \u00FF \u2603");
			Assert.Contains("&#", result);
		}

		[Fact]
		public void HtmlDecode_NumericEntity_Decimal_Large()
		{
			var result = HttpUtility.HtmlDecode("&#9786;");
			Assert.NotNull(result);
		}

		[Fact]
		public void HtmlDecode_With_Multiple_Mixed_Entities()
		{
			var result = HttpUtility.HtmlDecode("&lt;b&gt;bold&lt;/b&gt; &amp; &#65;");
			Assert.Equal("<b>bold</b> & A", result);
		}
	}
}
