using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class StringExtensionsTests
	{
		[Fact]
		public void HasValue_Returns_True_For_NonEmpty()
		{
			Assert.True("hello".HasValue());
		}

		[Fact]
		public void HasValue_Returns_False_For_Empty()
		{
			Assert.False("".HasValue());
		}

		[Fact]
		public void HasValue_Returns_False_For_Null()
		{
			string s = null;
			Assert.False(s.HasValue());
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
		public void RemoveUnderscoresAndDashes_Removes_Both()
		{
			Assert.Equal("abc def", "a_b-c d_e-f".RemoveUnderscoresAndDashes());
		}

		[Fact]
		public void RemoveSurroundingQuotes_Removes_Quotes()
		{
			Assert.Equal("hello", "\"hello\"".RemoveSurroundingQuotes());
		}

		[Fact]
		public void RemoveSurroundingQuotes_Preserves_String_Without_Quotes()
		{
			Assert.Equal("hello", "hello".RemoveSurroundingQuotes());
		}

		[Fact]
		public void RemoveSurroundingQuotes_Preserves_String_With_Only_Leading_Quote()
		{
			Assert.Equal("\"hello", "\"hello".RemoveSurroundingQuotes());
		}

		[Fact]
		public void ToPascalCase_Converts_Underscored()
		{
			var result = "some_test_string".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("SomeTestString", result);
		}

		[Fact]
		public void ToPascalCase_With_Single_Word()
		{
			var result = "hello".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void ToPascalCase_With_Empty_String()
		{
			var result = "".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("", result);
		}

		[Fact]
		public void ToPascalCase_With_Null_String()
		{
			string s = null;
			var result = s.ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Null(result);
		}

		[Fact]
		public void ToPascalCase_All_Uppercase()
		{
			var result = "HELLO".ToPascalCase(CultureInfo.InvariantCulture);
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void ToPascalCase_Without_Removing_Underscores()
		{
			var result = "some_test".ToPascalCase(false, CultureInfo.InvariantCulture);
			Assert.Contains("_", result);
		}

		[Fact]
		public void ToCamelCase_Converts_Underscored()
		{
			var result = "some_test_string".ToCamelCase(CultureInfo.InvariantCulture);
			Assert.Equal("someTestString", result);
		}

		[Fact]
		public void MakeInitialLowerCase_Converts()
		{
			Assert.Equal("hello", "Hello".MakeInitialLowerCase());
		}

		[Fact]
		public void IsUpperCase_Returns_True()
		{
			Assert.True("ABC".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Mixed()
		{
			Assert.False("AbC".IsUpperCase());
		}

		[Fact]
		public void IsUpperCase_Returns_False_For_Lower()
		{
			Assert.False("abc".IsUpperCase());
		}

		[Fact]
		public void AddUnderscores_Converts_PascalCase()
		{
			Assert.Equal("My_Property_Name", "MyPropertyName".AddUnderscores());
		}

		[Fact]
		public void AddDashes_Converts_PascalCase()
		{
			Assert.Equal("My-Property-Name", "MyPropertyName".AddDashes());
		}

		[Fact]
		public void GetNameVariants_Returns_Multiple_Variants()
		{
			var variants = "MyProperty".GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Contains("MyProperty", variants);
			Assert.Contains("myProperty", variants);
			Assert.Contains("myproperty", variants);
			Assert.Contains("My_Property", variants);
			Assert.Contains("my_property", variants);
			Assert.Contains("My-Property", variants);
			Assert.Contains("my-property", variants);
		}

		[Fact]
		public void GetNameVariants_Empty_String_Returns_Empty()
		{
			var variants = "".GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Empty(variants);
		}

		[Fact]
		public void GetNameVariants_Null_Returns_Empty()
		{
			string s = null;
			var variants = s.GetNameVariants(CultureInfo.InvariantCulture).ToList();
			Assert.Empty(variants);
		}

		[Fact]
		public void Matches_Returns_True_For_Match()
		{
			Assert.True("hello123".Matches(@"\d+"));
		}

		[Fact]
		public void Matches_Returns_False_For_No_Match()
		{
			Assert.False("hello".Matches(@"^\d+$"));
		}

		[Fact]
		public void UrlEncode_Encodes_Special_Characters()
		{
			var result = "hello world&foo=bar".UrlEncode();
			Assert.DoesNotContain(" ", result);
			Assert.Contains("%26", result);
		}

		[Fact]
		public void UrlDecode_Decodes_Encoded_Characters()
		{
			var encoded = "hello%20world";
			var result = encoded.UrlDecode();
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void ParseJsonDate_Unix_Timestamp()
		{
			var result = "1265686800".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_Date_Constructor()
		{
			var result = "new Date(1265686800000)".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_Iso8601_Format()
		{
			var result = "2010-02-08T11:11:11Z".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
			Assert.Equal(2, result.Month);
			Assert.Equal(8, result.Day);
		}

		[Fact]
		public void ParseJsonDate_WCF_Format()
		{
			var result = "\\/Date(1265686800000)\\/".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_With_Quotes()
		{
			var result = "\"2010-02-08T11:11:11Z\"".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_With_Newlines()
		{
			var result = "\n2010-02-08T11:11:11Z\r".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_WCF_Format_With_Timezone_Offset()
		{
			var result = "\\/Date(1265686800000+0000)\\/".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(2010, result.Year);
		}

		[Fact]
		public void ParseJsonDate_Invalid_Returns_Default()
		{
			var result = "not-a-date".ParseJsonDate(CultureInfo.InvariantCulture);
			Assert.Equal(default(DateTime), result);
		}

		[Fact]
		public void HtmlEncode_Encodes_Special_Characters()
		{
			var result = "<div>".HtmlEncode();
			Assert.Contains("&lt;", result);
		}

		[Fact]
		public void HtmlDecode_Decodes_Entities()
		{
			var result = "&lt;div&gt;".HtmlDecode();
			Assert.Equal("<div>", result);
		}
	}
}
