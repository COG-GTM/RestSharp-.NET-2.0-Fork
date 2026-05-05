using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using RestSharp.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class ReflectionExtensionsTests
	{
		[Fact]
		public void GetAttribute_From_Property_Returns_Attribute()
		{
			var prop = typeof(TestClassWithAttributes).GetProperty("Name");
			var attr = prop.GetAttribute<ObsoleteAttribute>();
			Assert.NotNull(attr);
		}

		[Fact]
		public void GetAttribute_From_Property_Returns_Null_When_Missing()
		{
			var prop = typeof(TestClassWithAttributes).GetProperty("Age");
			var attr = prop.GetAttribute<ObsoleteAttribute>();
			Assert.Null(attr);
		}

		[Fact]
		public void GetAttribute_From_Type_Returns_Attribute()
		{
			var attr = typeof(TestClassWithAttributes).GetAttribute<SerializableAttribute>();
			Assert.NotNull(attr);
		}

		[Fact]
		public void GetAttribute_From_Type_Returns_Null_When_Missing()
		{
			var attr = typeof(TestClassNoAttr).GetAttribute<SerializableAttribute>();
			Assert.Null(attr);
		}

		[Fact]
		public void IsSubclassOfRawGeneric_Returns_True()
		{
			Assert.True(typeof(StringList).IsSubclassOfRawGeneric(typeof(List<>)));
		}

		[Fact]
		public void IsSubclassOfRawGeneric_Returns_False()
		{
			Assert.False(typeof(string).IsSubclassOfRawGeneric(typeof(List<>)));
		}

		[Fact]
		public void ChangeType_Converts_String_To_Int()
		{
			var result = "42".ChangeType(typeof(int));
			Assert.Equal(42, result);
		}

		[Fact]
		public void ChangeType_Converts_Int_To_String()
		{
			var result = 42.ChangeType(typeof(string));
			Assert.Equal("42", result);
		}

		[Fact]
		public void ChangeType_With_Culture()
		{
			var result = 3.14.ChangeType(typeof(string), CultureInfo.InvariantCulture);
			Assert.Equal("3.14", result);
		}

		[Fact]
		public void FindEnumValue_Finds_By_Name()
		{
			var result = typeof(Method).FindEnumValue("GET", CultureInfo.InvariantCulture);
			Assert.Equal(Method.GET, result);
		}

		[Fact]
		public void FindEnumValue_Finds_Post()
		{
			var result = typeof(Method).FindEnumValue("POST", CultureInfo.InvariantCulture);
			Assert.Equal(Method.POST, result);
		}

		[Fact]
		public void FindEnumValue_Finds_Case_Insensitive()
		{
			var result = typeof(ParameterType).FindEnumValue("Cookie", CultureInfo.InvariantCulture);
			Assert.Equal(ParameterType.Cookie, result);
		}

		[Serializable]
		private class TestClassWithAttributes
		{
			[Obsolete]
			public string Name { get; set; }
			public int Age { get; set; }
		}

		private class TestClassNoAttr
		{
			public string Value { get; set; }
		}

		private class StringList : List<string> { }
	}
}
