using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class StringExtensionTests
	{
		[Fact]
		public void HasValue_Returns_True_For_NonEmpty_String()
		{
			Assert.True("hello".HasValue());
		}

		[Fact]
		public void HasValue_Returns_False_For_Null()
		{
			string s = null;
			Assert.False(s.HasValue());
		}

		[Fact]
		public void HasValue_Returns_False_For_Empty()
		{
			Assert.False("".HasValue());
		}

		[Fact]
		public void RemoveUnderscoresAndDashes_Removes_Underscores()
		{
			Assert.Equal("helloworld", "hello_world".RemoveUnderscoresAndDashes());
		}

		[Fact]
		public void RemoveUnderscoresAndDashes_Removes_Dashes()
		{
			Assert.Equal("helloworld", "hello-world".RemoveUnderscoresAndDashes());
		}

		[Fact]
		public void ToPascalCase_Converts_Underscore_Separated()
		{
			var result = "some_text_here".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("SomeTextHere", result);
		}

		[Fact]
		public void ToPascalCase_Returns_Null_For_Null()
		{
			string s = null;
			var result = s.ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Null(result);
		}

		[Fact]
		public void ToPascalCase_Returns_Empty_For_Empty()
		{
			var result = "".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("", result);
		}

		[Fact]
		public void ToPascalCase_With_Single_Lowercase_Word()
		{
			var result = "hello".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void ToPascalCase_With_All_Uppercase_Word()
		{
			var result = "HELLO".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void ToPascalCase_Without_Removing_Underscores()
		{
			var result = "some_text".ToPascalCase(false, CultureInfo.InvariantCulture);
			Assert.Contains("_", result);
		}

		[Fact]
		public void ToCamelCase_Converts_Underscore_Separated()
		{
			var result = "some_text_here".ToCamelCase(CultureInfo.InvariantCulture);
			Assert.Equal("someTextHere", result);
		}

		[Fact]
		public void MakeInitialLowerCase_Converts_First_Char()
		{
			Assert.Equal("hello", "Hello".MakeInitialLowerCase());
		}

		[Fact]
		public void IsUpperCase_Returns_True_For_All_Upper()
		{
			Assert.True("HELLO".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Mixed_Case()
		{
			Assert.False("Hello".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Lower()
		{
			Assert.False("hello".IsUpperCase());
		}

		[Fact]
		public void AddUnderscores_Converts_PascalCase()
		{
			Assert.Equal("Some_Text_Here", "SomeTextHere".AddUnderscores());
		}

		[Fact]
		public void AddDashes_Converts_PascalCase()
		{
			Assert.Equal("Some-Text-Here", "SomeTextHere".AddDashes());
		}

		[Fact]
		public void Matches_Returns_True_For_Matching_Pattern()
		{
			Assert.True("hello123".Matches(@"\d+"));
		}

		[Fact]
		public void Matches_Returns_False_For_Non_Matching()
		{
			Assert.False("hello".Matches(@"^\d+$"));
		}

		[Fact]
		public void GetNameVariants_Returns_Multiple_Variants()
		{
			var variants = "SomeName".GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.True(variants.Count >= 7);
			Assert.Contains("SomeName", variants);
			Assert.Contains("someName", variants);
			Assert.Contains("somename", variants);
		}

		[Fact]
		public void GetNameVariants_Returns_Empty_For_Null()
		{
			string s = null;
			var variants = s.GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Empty(variants);
		}

		[Fact]
		public void GetNameVariants_Returns_Empty_For_Empty_String()
		{
			var variants = "".GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Empty(variants);
		}

		[Fact]
		public void UrlEncode_Encodes_Special_Characters()
		{
			var result = "hello world&foo=bar".UrlEncode();
			Assert.DoesNotContain(" ", result);
			Assert.Contains("%26", result);
		}

		[Fact]
		public void UrlDecode_Decodes_Encoded_String()
		{
			var result = "hello%20world".UrlDecode();
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void HtmlEncode_Encodes_Special_Characters()
		{
			var result = "<div>test</div>".HtmlEncode();
			Assert.DoesNotContain("<", result);
		}

		[Fact]
		public void HtmlDecode_Decodes_Encoded_String()
		{
			var result = "&lt;div&gt;test&lt;/div&gt;".HtmlDecode();
			Assert.Equal("<div>test</div>", result);
		}

		[Fact]
		public void RemoveSurroundingQuotes_Removes_Double_Quotes()
		{
			Assert.Equal("hello", "\"hello\"".RemoveSurroundingQuotes());
		}

		[Fact]
		public void RemoveSurroundingQuotes_Leaves_Unquoted()
		{
			Assert.Equal("hello", "hello".RemoveSurroundingQuotes());
		}

		[Fact]
		public void ParseJsonDate_Parses_Iso8601_Date()
		{
			var result = "2023-01-15T10:30:00Z".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
			Assert.Equal(2023, result.Year);
			Assert.Equal(1, result.Month);
			Assert.Equal(15, result.Day);
		}

		[Fact]
		public void ParseJsonDate_Parses_Millisecond_Timestamp()
		{
			// The escaped JSON format: \/Date(...)\/ 
			var result = "\\/Date(1234567890000)\\/".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
		}

		[Fact]
		public void ParseJsonDate_Parses_Millisecond_Timestamp_With_Offset()
		{
			var result = "\\/Date(1234567890000+0530)\\/".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
		}

		[Fact]
		public void ParseJsonDate_Parses_Millisecond_Timestamp_With_Negative_Offset()
		{
			var result = "\\/Date(1234567890000-0500)\\/".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
		}

		[Fact]
		public void ParseJsonDate_Handles_New_Date_Format()
		{
			var result = "new Date(1234567890000)".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.NotEqual(default(DateTime), result);
		}

		[Fact]
		public void AddUnderscores_Handles_Consecutive_Uppercase()
		{
			// e.g. XMLParser -> XML_Parser
			var result = "XMLParser".AddUnderscores();
			Assert.Contains("_", result);
		}

		[Fact]
		public void AddDashes_Handles_Spaces()
		{
			var result = "Hello World".AddDashes();
			Assert.Contains("-", result);
		}
	}

	public class MiscExtensionTests
	{
		[Fact]
		public void ReadAsBytes_Reads_Stream_To_Bytes()
		{
			var data = new byte[] { 1, 2, 3, 4, 5 };
			using (var stream = new MemoryStream(data))
			{
				var result = stream.ReadAsBytes();
				Assert.Equal(data, result);
			}
		}

		[Fact]
		public void SaveAs_Writes_Bytes_To_File()
		{
			var data = new byte[] { 10, 20, 30 };
			var tempFile = Path.GetTempFileName();
			try
			{
				data.SaveAs(tempFile);
				var readBack = File.ReadAllBytes(tempFile);
				Assert.Equal(data, readBack);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public void AsString_Converts_Bytes_To_String()
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
			var result = bytes.AsString();
			Assert.Equal("hello", result);
		}
	}

	public class XmlExtensionTests
	{
		[Fact]
		public void AsNamespaced_Without_Namespace_Returns_Name()
		{
			var result = "element".AsNamespaced("");
			Assert.Equal("element", result.LocalName);
		}

		[Fact]
		public void AsNamespaced_With_Namespace_Returns_Namespaced_Name()
		{
			var result = "element".AsNamespaced("http://example.com/ns");
			Assert.Equal("element", result.LocalName);
			Assert.Equal("http://example.com/ns", result.NamespaceName);
		}
	}

	public class ReflectionExtensionTests
	{
		[Fact]
		public void IsSubclassOfRawGeneric_Returns_True_For_Derived_Generic()
		{
			Assert.True(typeof(System.Collections.Generic.List<string>)
				.IsSubclassOfRawGeneric(typeof(System.Collections.Generic.List<>)));
		}

		[Fact]
		public void IsSubclassOfRawGeneric_Returns_False_For_Non_Derived()
		{
			Assert.False(typeof(string).IsSubclassOfRawGeneric(typeof(System.Collections.Generic.List<>)));
		}

		[Fact]
		public void ChangeType_Converts_String_To_Int()
		{
			var result = "42".ChangeType(typeof(int));
			Assert.Equal(42, result);
		}

		[Fact]
		public void ChangeType_With_Culture_Converts_String_To_Double()
		{
			var result = "3.14".ChangeType(typeof(double), CultureInfo.InvariantCulture);
			Assert.Equal(3.14, result);
		}

		[Fact]
		public void FindEnumValue_Finds_By_Name()
		{
			var result = typeof(Method).FindEnumValue("GET", CultureInfo.InvariantCulture);
			Assert.Equal(Method.GET, result);
		}

		[Fact]
		public void FindEnumValue_Finds_CaseInsensitive()
		{
			var result = typeof(Method).FindEnumValue("get", CultureInfo.InvariantCulture);
			Assert.Equal(Method.GET, result);
		}

		[Fact]
		public void GetAttribute_Returns_Null_When_Not_Present()
		{
			var attr = typeof(string).GetAttribute<ObsoleteAttribute>();
			Assert.Null(attr);
		}
	}
}
