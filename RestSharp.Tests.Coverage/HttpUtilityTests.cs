using System.Text;
using RestSharp.Contrib;
using Xunit;

namespace RestSharp.Tests
{
	public class HttpUtilityTests
	{
		// UrlEncode with byte arrays
		[Fact]
		public void UrlEncode_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncode(bytes, 0, bytes.Length);
			Assert.Equal("hello+world", result);
		}

		[Fact]
		public void UrlEncode_Bytes_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.UrlEncode((byte[])null, 0, 0));
		}

		[Fact]
		public void UrlDecode_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("hello+world");
			var result = HttpUtility.UrlDecode(bytes, 0, bytes.Length, Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlDecode_Bytes_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.UrlDecode((byte[])null, 0, 0, Encoding.UTF8));
		}

		[Fact]
		public void UrlEncode_String_With_Encoding()
		{
			var result = HttpUtility.UrlEncode("hello world", Encoding.UTF8);
			Assert.Equal("hello+world", result);
		}

		[Fact]
		public void UrlDecode_String_With_Encoding()
		{
			var result = HttpUtility.UrlDecode("hello+world", Encoding.UTF8);
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void UrlEncodeToBytes_String()
		{
			var result = HttpUtility.UrlEncodeToBytes("hello");
			Assert.NotNull(result);
			Assert.Equal("hello", Encoding.ASCII.GetString(result));
		}

		[Fact]
		public void UrlEncodeToBytes_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.UrlEncodeToBytes((string)null));
		}

		[Fact]
		public void UrlDecodeToBytes_String()
		{
			var result = HttpUtility.UrlDecodeToBytes("hello");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.UrlDecodeToBytes((string)null));
		}

		[Fact]
		public void UrlEncodeToBytes_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			var result = HttpUtility.UrlEncodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncodeToBytes_Bytes_Null()
		{
			Assert.Null(HttpUtility.UrlEncodeToBytes((byte[])null));
		}

		[Fact]
		public void UrlDecodeToBytes_Bytes()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			var result = HttpUtility.UrlDecodeToBytes(bytes);
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlDecodeToBytes_Bytes_Null()
		{
			Assert.Null(HttpUtility.UrlDecodeToBytes((byte[])null));
		}

		[Fact]
		public void UrlEncodeToBytes_Bytes_WithRange()
		{
			var bytes = Encoding.UTF8.GetBytes("hello world");
			var result = HttpUtility.UrlEncodeToBytes(bytes, 0, bytes.Length);
			Assert.NotNull(result);
			Assert.Contains((byte)'+', result);
		}

		[Fact]
		public void UrlDecodeToBytes_Bytes_WithRange()
		{
			var bytes = Encoding.UTF8.GetBytes("hello+world");
			var result = HttpUtility.UrlDecodeToBytes(bytes, 0, bytes.Length);
			Assert.NotNull(result);
			Assert.Contains((byte)' ', result);
		}

		[Fact]
		public void HtmlEncode_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.HtmlEncode(null));
		}

		[Fact]
		public void HtmlDecode_Null_Returns_Null()
		{
			Assert.Null(HttpUtility.HtmlDecode(null));
		}

		[Fact]
		public void HtmlEncode_NoSpecialChars()
		{
			Assert.Equal("hello", HttpUtility.HtmlEncode("hello"));
		}

		[Fact]
		public void HtmlEncode_AllSpecialChars()
		{
			Assert.Equal("&lt;&gt;&amp;&quot;", HttpUtility.HtmlEncode("<>&\""));
		}

		[Fact]
		public void HtmlDecode_Entities()
		{
			Assert.Equal("<>&\"", HttpUtility.HtmlDecode("&lt;&gt;&amp;&quot;"));
		}

		[Fact]
		public void HtmlDecode_Hex_NumericEntity()
		{
			var result = HttpUtility.HtmlDecode("&#x41;");
			Assert.Equal("A", result);
		}

		[Fact]
		public void UrlEncode_Unicode()
		{
			var result = HttpUtility.UrlEncode("日本語");
			Assert.NotNull(result);
			Assert.Contains("%", result);
		}

		[Fact]
		public void UrlDecode_PercentEncoded()
		{
			var result = HttpUtility.UrlDecode("%E6%97%A5%E6%9C%AC%E8%AA%9E");
			Assert.Equal("日本語", result);
		}

		[Fact]
		public void ParseQueryString_Multiple_Values()
		{
			var result = HttpUtility.ParseQueryString("a=1&b=2&c=3");
			Assert.Equal(3, result.Count);
			Assert.Equal("1", result["a"]);
			Assert.Equal("3", result["c"]);
		}

		[Fact]
		public void ParseQueryString_Encoded_Values()
		{
			var result = HttpUtility.ParseQueryString("name=hello+world&val=a%26b");
			Assert.Equal("hello world", result["name"]);
			Assert.Equal("a&b", result["val"]);
		}

		[Fact]
		public void ParseQueryString_No_Value()
		{
			var result = HttpUtility.ParseQueryString("key");
			Assert.NotNull(result);
		}

		[Fact]
		public void UrlEncode_Empty_Returns_Empty()
		{
			Assert.Equal("", HttpUtility.UrlEncode(""));
		}

		[Fact]
		public void UrlDecode_Empty_Returns_Empty()
		{
			Assert.Equal("", HttpUtility.UrlDecode(""));
		}

		[Fact]
		public void HtmlEncode_HighUnicode()
		{
			var result = HttpUtility.HtmlEncode("©");
			Assert.NotNull(result);
		}

		[Fact]
		public void ParseQueryString_Duplicate_Keys()
		{
			var result = HttpUtility.ParseQueryString("a=1&a=2");
			Assert.NotNull(result["a"]);
		}
	}
}
