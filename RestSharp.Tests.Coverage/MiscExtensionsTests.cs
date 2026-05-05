using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class MiscExtensionsTests
	{
		[Fact]
		public void ReadAsBytes_Reads_Stream()
		{
			var data = new byte[] { 1, 2, 3, 4, 5 };
			using (var stream = new MemoryStream(data))
			{
				var result = stream.ReadAsBytes();
				Assert.Equal(data, result);
			}
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

		[Fact]
		public void CopyTo_Copies_Stream_Content()
		{
			var data = new byte[] { 10, 20, 30, 40, 50 };
			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream())
			{
				input.CopyTo(output);
				Assert.Equal(data, output.ToArray());
			}
		}

		[Fact]
		public void CopyTo_Empty_Stream()
		{
			using (var input = new MemoryStream())
			using (var output = new MemoryStream())
			{
				input.CopyTo(output);
				Assert.Empty(output.ToArray());
			}
		}

		[Fact]
		public void AsString_JToken_String_Type()
		{
			var token = JToken.Parse("\"hello\"");
			Assert.Equal("hello", token.AsString());
		}

		[Fact]
		public void AsString_JToken_Number_Type()
		{
			var token = JToken.Parse("42");
			Assert.Equal("42", token.AsString());
		}

		[Fact]
		public void AsString_JToken_With_Culture()
		{
			var token = new JValue(3.14);
			var result = token.AsString(CultureInfo.InvariantCulture);
			Assert.Equal("3.14", result);
		}

		[Fact]
		public void AsString_JToken_String_With_Culture()
		{
			var token = JToken.Parse("\"test\"");
			var result = token.AsString(CultureInfo.InvariantCulture);
			Assert.Equal("test", result);
		}

		[Fact]
		public void AsString_ByteArray_Utf8()
		{
			var bytes = Encoding.UTF8.GetBytes("Hello World");
			var result = bytes.AsString();
			Assert.Equal("Hello World", result);
		}

		[Fact]
		public void AsString_ByteArray_Null_Returns_Empty()
		{
			byte[] bytes = null;
			var result = bytes.AsString();
			Assert.Equal("", result);
		}

		[Fact]
		public void SaveAs_Writes_File()
		{
			var tmpPath = Path.GetTempFileName();
			try
			{
				var data = new byte[] { 1, 2, 3, 4 };
				data.SaveAs(tmpPath);
				var read = File.ReadAllBytes(tmpPath);
				Assert.Equal(data, read);
			}
			finally
			{
				if (File.Exists(tmpPath))
					File.Delete(tmpPath);
			}
		}
	}
}
