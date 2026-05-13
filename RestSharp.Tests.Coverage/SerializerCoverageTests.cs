using System;
using System.Collections.Generic;
using System.Globalization;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class JsonSerializerTests
	{
		[Fact]
		public void Serialize_Simple_Object()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = "test", Value = 42 });
			Assert.Contains("test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void ContentType_Defaults_To_ApplicationJson()
		{
			var serializer = new JsonSerializer();
			Assert.Equal("application/json", serializer.ContentType);
		}

		[Fact]
		public void ContentType_Can_Be_Set()
		{
			var serializer = new JsonSerializer();
			serializer.ContentType = "text/json";
			Assert.Equal("text/json", serializer.ContentType);
		}

		[Fact]
		public void Serialize_List_Object()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new List<string> { "a", "b", "c" });
			Assert.Contains("a", result);
			Assert.Contains("b", result);
		}

		[Fact]
		public void Serialize_Null_Returns_Null_Json()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(null);
			Assert.Equal("null", result);
		}

		[Fact]
		public void Serialize_Complex_Object()
		{
			var serializer = new JsonSerializer();
			var obj = new SerializableObj
			{
				Name = "Foo",
				Age = 25,
				Items = new List<string> { "x", "y" }
			};
			var result = serializer.Serialize(obj);
			Assert.Contains("Foo", result);
			Assert.Contains("25", result);
		}

		[Fact]
		public void RootElement_Can_Be_Set()
		{
			var serializer = new JsonSerializer();
			serializer.RootElement = "data";
			Assert.Equal("data", serializer.RootElement);
		}

		[Fact]
		public void Namespace_Can_Be_Set()
		{
			var serializer = new JsonSerializer();
			serializer.Namespace = "ns";
			Assert.Equal("ns", serializer.Namespace);
		}

		[Fact]
		public void DateFormat_Can_Be_Set()
		{
			var serializer = new JsonSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
		}
	}

	public class XmlSerializerCoverageTests
	{
		[Fact]
		public void Default_ContentType_Is_ApplicationXml()
		{
			var serializer = new XmlSerializer();
			Assert.Equal("text/xml", serializer.ContentType);
		}

		[Fact]
		public void Serialize_Simple_Object()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new SimpleXmlObj { Name = "test", Value = 42 });
			Assert.Contains("test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void RootElement_Override()
		{
			var serializer = new XmlSerializer();
			serializer.RootElement = "CustomRoot";
			var result = serializer.Serialize(new SimpleXmlObj { Name = "test" });
			Assert.Contains("CustomRoot", result);
		}

		[Fact]
		public void Namespace_Is_Applied()
		{
			var serializer = new XmlSerializer();
			serializer.Namespace = "http://example.com/ns";
			var result = serializer.Serialize(new SimpleXmlObj { Name = "test" });
			Assert.Contains("http://example.com/ns", result);
		}

		[Fact]
		public void Serialize_With_Nullable_Property()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithNullable { Value = null, Name = "test" });
			Assert.Contains("test", result);
		}

		[Fact]
		public void Serialize_With_Nullable_Property_Set()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithNullable { Value = 42, Name = "test" });
			Assert.Contains("42", result);
		}

		[Fact]
		public void Serialize_With_DateTime()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithDate { Date = new DateTime(2023, 6, 15) });
			Assert.Contains("2023", result);
		}

		[Fact]
		public void Serialize_With_DateFormat()
		{
			var serializer = new XmlSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			var result = serializer.Serialize(new ObjWithDate { Date = new DateTime(2023, 6, 15) });
			Assert.Contains("2023-06-15", result);
		}

		[Fact]
		public void Serialize_With_Enum()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithEnum { Status = Method.POST });
			Assert.Contains("POST", result);
		}

		[Fact]
		public void Serialize_With_Guid()
		{
			var serializer = new XmlSerializer();
			var guid = Guid.NewGuid();
			var result = serializer.Serialize(new ObjWithGuid { Id = guid });
			Assert.Contains(guid.ToString(), result);
		}

		[Fact]
		public void Serialize_With_Boolean()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithBool { Active = true, Disabled = false });
			Assert.Contains("true", result.ToLower());
			Assert.Contains("false", result.ToLower());
		}

		[Fact]
		public void Serialize_With_List()
		{
			var serializer = new XmlSerializer();
			var obj = new ObjWithList
			{
				Name = "parent",
				Items = new List<SimpleXmlObj>
				{
					new SimpleXmlObj { Name = "child1", Value = 1 },
					new SimpleXmlObj { Name = "child2", Value = 2 }
				}
			};
			var result = serializer.Serialize(obj);
			Assert.Contains("child1", result);
			Assert.Contains("child2", result);
		}

		[Fact]
		public void Serialize_With_Decimal()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithDecimal { Price = 19.95m });
			Assert.Contains("19.95", result);
		}

		[Fact]
		public void Serialize_With_Double()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithDouble { Rate = 3.14 });
			Assert.Contains("3.14", result);
		}

		[Fact]
		public void Serialize_With_Long()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithLong { BigNum = long.MaxValue });
			Assert.Contains(long.MaxValue.ToString(), result);
		}

		[Fact]
		public void Serialize_With_TimeSpan()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new ObjWithTimeSpan { Duration = TimeSpan.FromHours(1) });
			Assert.NotNull(result);
		}

		[Fact]
		public void ContentType_Can_Be_Set()
		{
			var serializer = new XmlSerializer();
			serializer.ContentType = "application/xml";
			Assert.Equal("application/xml", serializer.ContentType);
		}
	}

	public class DotNetXmlSerializerTests
	{
		[Fact]
		public void Default_ContentType_Is_ApplicationXml()
		{
			var serializer = new DotNetXmlSerializer();
			Assert.Equal("application/xml", serializer.ContentType);
		}

		[Fact]
		public void Constructor_With_Namespace()
		{
			var serializer = new DotNetXmlSerializer("http://example.com");
			Assert.Equal("http://example.com", serializer.Namespace);
		}

		[Fact]
		public void Serialize_Simple_Object()
		{
			var serializer = new DotNetXmlSerializer();
			var result = serializer.Serialize(new SimpleXmlObj { Name = "test", Value = 42 });
			Assert.Contains("test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void Encoding_Defaults_To_UTF8()
		{
			var serializer = new DotNetXmlSerializer();
			Assert.Equal(System.Text.Encoding.UTF8, serializer.Encoding);
		}

		[Fact]
		public void Properties_Can_Be_Set()
		{
			var serializer = new DotNetXmlSerializer();
			serializer.RootElement = "Root";
			serializer.DateFormat = "yyyy-MM-dd";
			serializer.ContentType = "text/xml";

			Assert.Equal("Root", serializer.RootElement);
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
			Assert.Equal("text/xml", serializer.ContentType);
		}
	}

	public class DotNetXmlDeserializerTests
	{
		[Fact]
		public void Deserialize_Simple_Object()
		{
			var deserializer = new RestSharp.Deserializers.DotNetXmlDeserializer();
			var xml = "<?xml version=\"1.0\"?><SimpleXmlObj xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Name>test</Name><Value>42</Value></SimpleXmlObj>";
			var response = new RestResponse { Content = xml };
			var result = deserializer.Deserialize<SimpleXmlObj>(response);

			Assert.Equal("test", result.Name);
			Assert.Equal(42, result.Value);
		}

		[Fact]
		public void Deserialize_Empty_Content_Returns_Default()
		{
			var deserializer = new RestSharp.Deserializers.DotNetXmlDeserializer();
			var response = new RestResponse { Content = "" };
			var result = deserializer.Deserialize<SimpleXmlObj>(response);

			Assert.Null(result);
		}

		[Fact]
		public void Deserialize_Null_Content_Returns_Default()
		{
			var deserializer = new RestSharp.Deserializers.DotNetXmlDeserializer();
			var response = new RestResponse { Content = null };
			var result = deserializer.Deserialize<SimpleXmlObj>(response);

			Assert.Null(result);
		}

		[Fact]
		public void Properties_Can_Be_Set()
		{
			var deserializer = new RestSharp.Deserializers.DotNetXmlDeserializer();
			deserializer.DateFormat = "yyyy-MM-dd";
			deserializer.Namespace = "http://example.com";
			deserializer.RootElement = "data";

			Assert.Equal("yyyy-MM-dd", deserializer.DateFormat);
			Assert.Equal("http://example.com", deserializer.Namespace);
			Assert.Equal("data", deserializer.RootElement);
		}
	}

	public class SerializeAsAttributeTests
	{
		[Fact]
		public void Default_NameStyle_Is_AsIs()
		{
			var attr = new SerializeAsAttribute();
			Assert.Equal(NameStyle.AsIs, attr.NameStyle);
		}

		[Fact]
		public void Default_Index_Is_MaxValue()
		{
			var attr = new SerializeAsAttribute();
			Assert.Equal(int.MaxValue, attr.Index);
		}

		[Fact]
		public void TransformName_AsIs_Returns_Input()
		{
			var attr = new SerializeAsAttribute();
			Assert.Equal("TestName", attr.TransformName("TestName"));
		}

		[Fact]
		public void TransformName_CamelCase_Transforms()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.CamelCase };
			var result = attr.TransformName("TestName");
			Assert.Equal("testName", result);
		}

		[Fact]
		public void TransformName_PascalCase_Transforms()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.PascalCase };
			var result = attr.TransformName("test_name");
			Assert.Equal("TestName", result);
		}

		[Fact]
		public void TransformName_LowerCase_Transforms()
		{
			var attr = new SerializeAsAttribute { NameStyle = NameStyle.LowerCase };
			var result = attr.TransformName("TestName");
			Assert.Equal("testname", result);
		}

		[Fact]
		public void TransformName_With_Custom_Name()
		{
			var attr = new SerializeAsAttribute { Name = "custom", NameStyle = NameStyle.LowerCase };
			var result = attr.TransformName("OriginalName");
			Assert.Equal("custom", result);
		}

		[Fact]
		public void Attribute_Property_Can_Be_Set()
		{
			var attr = new SerializeAsAttribute { Attribute = true };
			Assert.True(attr.Attribute);
		}

		[Fact]
		public void Culture_Defaults_To_InvariantCulture()
		{
			var attr = new SerializeAsAttribute();
			Assert.Equal(CultureInfo.InvariantCulture, attr.Culture);
		}
	}

	public class SimpleXmlObj
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class ObjWithNullable
	{
		public string Name { get; set; }
		public int? Value { get; set; }
	}

	public class ObjWithDate
	{
		public DateTime Date { get; set; }
	}

	public class ObjWithEnum
	{
		public Method Status { get; set; }
	}

	public class ObjWithGuid
	{
		public Guid Id { get; set; }
	}

	public class ObjWithBool
	{
		public bool Active { get; set; }
		public bool Disabled { get; set; }
	}

	public class ObjWithList
	{
		public string Name { get; set; }
		public List<SimpleXmlObj> Items { get; set; }
	}

	public class ObjWithDecimal
	{
		public decimal Price { get; set; }
	}

	public class ObjWithDouble
	{
		public double Rate { get; set; }
	}

	public class ObjWithLong
	{
		public long BigNum { get; set; }
	}

	public class ObjWithTimeSpan
	{
		public TimeSpan Duration { get; set; }
	}

	public class SerializableObj
	{
		public string Name { get; set; }
		public int Age { get; set; }
		public List<string> Items { get; set; }
	}
}
