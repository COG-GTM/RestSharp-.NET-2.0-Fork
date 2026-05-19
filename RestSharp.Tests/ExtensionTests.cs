using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class ExtensionTests
	{
		private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

		// UrlEncode / UrlDecode
		[Fact]
		public void UrlEncode_Encodes_Spaces()
		{
			Assert.Equal("hello%20world", "hello world".UrlEncode());
		}

		[Fact]
		public void UrlDecode_Decodes_Encoded_String()
		{
			Assert.Equal("hello world", "hello%20world".UrlDecode());
		}

		[Fact]
		public void UrlEncode_UrlDecode_Roundtrip()
		{
			var original = "test value&key=1+2";
			var encoded = original.UrlEncode();
			Assert.Equal(original, encoded.UrlDecode());
		}

		// HtmlEncode / HtmlDecode
		[Fact]
		public void HtmlEncode_Encodes_Special_Characters()
		{
			var result = "<div>test</div>".HtmlEncode();
			Assert.Contains("&lt;", result);
			Assert.Contains("&gt;", result);
		}

		[Fact]
		public void HtmlDecode_Decodes_Encoded_String()
		{
			Assert.Equal("<div>", "&lt;div&gt;".HtmlDecode());
		}

		// HasValue
		[Fact]
		public void HasValue_Returns_False_For_Null()
		{
			string input = null;
			Assert.False(input.HasValue());
		}

		[Fact]
		public void HasValue_Returns_False_For_Empty()
		{
			Assert.False("".HasValue());
		}

		[Fact]
		public void HasValue_Returns_True_For_NonEmpty()
		{
			Assert.True("test".HasValue());
		}

		// RemoveUnderscoresAndDashes
		[Fact]
		public void RemoveUnderscoresAndDashes_Removes_Both()
		{
			Assert.Equal("helloworld", "hello_world".RemoveUnderscoresAndDashes());
			Assert.Equal("helloworld", "hello-world".RemoveUnderscoresAndDashes());
			Assert.Equal("helloworld", "hello_-world".RemoveUnderscoresAndDashes());
		}

		// ParseJsonDate
		[Fact]
		public void ParseJsonDate_Parses_Unix_Timestamp()
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var result = "0".ParseJsonDate(Invariant);
			Assert.Equal(epoch, result);
		}

		[Fact]
		public void ParseJsonDate_Parses_Date_Format()
		{
			var result = "\\/Date(1234567890000)\\/".ParseJsonDate(Invariant);
			Assert.True(result.Year >= 2009);
		}

		[Fact]
		public void ParseJsonDate_Parses_New_Date_Format()
		{
			var result = "new Date(1234567890000)".ParseJsonDate(Invariant);
			Assert.True(result.Year >= 2009);
		}

		[Fact]
		public void ParseJsonDate_Parses_ISO_Format()
		{
			var result = "2023-06-15T10:30:00Z".ParseJsonDate(Invariant);
			Assert.Equal(2023, result.Year);
			Assert.Equal(6, result.Month);
			Assert.Equal(15, result.Day);
		}

		[Fact]
		public void ParseJsonDate_Parses_Date_With_Timezone_Offset()
		{
			var result = "\\/Date(1234567890000+0530)\\/".ParseJsonDate(Invariant);
			Assert.True(result.Year >= 2009);
		}

		// RemoveSurroundingQuotes
		[Fact]
		public void RemoveSurroundingQuotes_Removes_Quotes()
		{
			Assert.Equal("test", "\"test\"".RemoveSurroundingQuotes());
		}

		[Fact]
		public void RemoveSurroundingQuotes_No_Quotes_Unchanged()
		{
			Assert.Equal("test", "test".RemoveSurroundingQuotes());
		}

		// ToPascalCase
		[Fact]
		public void ToPascalCase_With_Underscores()
		{
			Assert.Equal("SomeText", "some_text".ToPascalCase(Invariant));
		}

		[Fact]
		public void ToPascalCase_Without_Underscores()
		{
			Assert.Equal("SomeText", "some_text".ToPascalCase(false, Invariant));
			Assert.Contains("_", "some_text".ToPascalCase(false, Invariant));
		}

		[Fact]
		public void ToPascalCase_Single_Word()
		{
			Assert.Equal("Test", "test".ToPascalCase(Invariant));
		}

		[Fact]
		public void ToPascalCase_Empty_String()
		{
			Assert.Equal("", "".ToPascalCase(Invariant));
		}

		[Fact]
		public void ToPascalCase_Null_Returns_Null()
		{
			Assert.Null(((string)null).ToPascalCase(Invariant));
		}

		// ToCamelCase
		[Fact]
		public void ToCamelCase_Converts_Correctly()
		{
			Assert.Equal("someText", "some_text".ToCamelCase(Invariant));
		}

		// MakeInitialLowerCase
		[Fact]
		public void MakeInitialLowerCase_Converts()
		{
			Assert.Equal("hello", "Hello".MakeInitialLowerCase());
		}

		// IsUpperCase
		[Fact]
		public void IsUpperCase_Returns_True_For_All_Upper()
		{
			Assert.True("HELLO".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Mixed()
		{
			Assert.False("Hello".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Lower()
		{
			Assert.False("hello".IsUpperCase());
		}

		// AddUnderscores
		[Fact]
		public void AddUnderscores_To_PascalCase()
		{
			Assert.Equal("Some_Text", "SomeText".AddUnderscores());
		}

		[Fact]
		public void AddUnderscores_To_Complex_PascalCase()
		{
			Assert.Equal("Some_TEXT_Value", "SomeTEXTValue".AddUnderscores());
		}

		// AddDashes
		[Fact]
		public void AddDashes_To_PascalCase()
		{
			Assert.Equal("Some-Text", "SomeText".AddDashes());
		}

		// GetNameVariants
		[Fact]
		public void GetNameVariants_Returns_Expected_Variants()
		{
			var variants = "SomeText".GetNameVariants(Invariant).ToList();
			Assert.True(variants.Count >= 7);
			Assert.Contains("SomeText", variants);
			Assert.Contains("someText", variants);
			Assert.Contains("sometext", variants);
			Assert.Contains("Some_Text", variants);
			Assert.Contains("some_text", variants);
			Assert.Contains("Some-Text", variants);
			Assert.Contains("some-text", variants);
		}

		[Fact]
		public void GetNameVariants_Empty_Returns_None()
		{
			var variants = "".GetNameVariants(Invariant).ToList();
			Assert.Equal(0, variants.Count);
		}

		[Fact]
		public void GetNameVariants_Null_Returns_None()
		{
			var variants = ((string)null).GetNameVariants(Invariant).ToList();
			Assert.Equal(0, variants.Count);
		}

		// Matches
		[Fact]
		public void Matches_Returns_True_For_Match()
		{
			Assert.True("hello123".Matches(@"\d+"));
		}

		[Fact]
		public void Matches_Returns_False_For_No_Match()
		{
			Assert.False("hello".Matches(@"\d+"));
		}

		// MiscExtensions
		[Fact]
		public void ReadAsBytes_Reads_Stream()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			using (var stream = new MemoryStream(bytes))
			{
				var result = stream.ReadAsBytes();
				Assert.Equal(bytes.Length, result.Length);
				Assert.Equal(bytes[0], result[0]);
			}
		}

		[Fact]
		public void CopyTo_Copies_Stream()
		{
			var data = Encoding.UTF8.GetBytes("test data");
			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream())
			{
				input.CopyTo(output);
				Assert.Equal(data.Length, output.Length);
			}
		}

		[Fact]
		public void SaveAs_Writes_File()
		{
			var path = Path.GetTempFileName();
			try
			{
				var data = Encoding.UTF8.GetBytes("test content");
				data.SaveAs(path);
				var result = File.ReadAllText(path);
				Assert.Equal("test content", result);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Fact]
		public void AsString_Null_Bytes_Returns_Empty()
		{
			byte[] buffer = null;
			Assert.Equal("", buffer.AsString());
		}

		[Fact]
		public void AsString_Bytes_Returns_String()
		{
			var bytes = Encoding.UTF8.GetBytes("hello");
			Assert.Equal("hello", bytes.AsString());
		}

		[Fact]
		public void AsString_JToken_String()
		{
			var token = new JValue("hello");
			Assert.Equal("hello", token.AsString());
		}

		[Fact]
		public void AsString_JToken_NonString()
		{
			var token = new JValue(42);
			Assert.Equal("42", token.AsString());
		}

		// ReflectionExtensions
		[Fact]
		public void GetAttribute_Returns_Attribute_From_Type()
		{
			var attr = typeof(TestAttributeClass).GetAttribute<SerializableAttribute>();
			Assert.NotNull(attr);
		}

		[Fact]
		public void GetAttribute_Returns_Null_When_Not_Present()
		{
			var attr = typeof(TestNoAttributeClass).GetAttribute<SerializableAttribute>();
			Assert.Null(attr);
		}

		[Fact]
		public void IsSubclassOfRawGeneric_Returns_True()
		{
			Assert.True(typeof(List<string>).IsSubclassOfRawGeneric(typeof(List<>)));
		}

		[Fact]
		public void IsSubclassOfRawGeneric_Returns_False()
		{
			Assert.False(typeof(string).IsSubclassOfRawGeneric(typeof(List<>)));
		}

		[Fact]
		public void ChangeType_Converts_String_To_Int()
		{
			var result = ((object)"42").ChangeType(typeof(int));
			Assert.Equal(42, result);
		}

		[Fact]
		public void ChangeType_With_Culture()
		{
			var result = ((object)"42").ChangeType(typeof(int), Invariant);
			Assert.Equal(42, result);
		}

		[Fact]
		public void FindEnumValue_Finds_Value()
		{
			var result = typeof(Method).FindEnumValue("GET", Invariant);
			Assert.Equal(Method.GET, result);
		}

		[Serializable]
		public class TestAttributeClass { }
		public class TestNoAttributeClass { }
	}
}
