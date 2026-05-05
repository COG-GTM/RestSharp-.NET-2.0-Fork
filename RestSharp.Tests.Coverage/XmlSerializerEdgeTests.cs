using System;
using System.Collections.Generic;
using System.Globalization;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests
{
	public class XmlSerializerEdgeTests
	{
		[Fact]
		public void XmlSerializer_Default_ContentType()
		{
			var serializer = new XmlSerializer();
			Assert.Equal("text/xml", serializer.ContentType);
		}

		[Fact]
		public void XmlSerializer_Namespace_Property()
		{
			var serializer = new XmlSerializer();
			serializer.Namespace = "http://example.com/ns";
			Assert.Equal("http://example.com/ns", serializer.Namespace);
		}

		[Fact]
		public void XmlSerializer_DateFormat_Property()
		{
			var serializer = new XmlSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
		}

		[Fact]
		public void XmlSerializer_RootElement_Property()
		{
			var serializer = new XmlSerializer();
			serializer.RootElement = "Root";
			Assert.Equal("Root", serializer.RootElement);
		}

		[Fact]
		public void XmlSerializer_Serialize_Simple_Object()
		{
			var serializer = new XmlSerializer();
			var result = serializer.Serialize(new SimpleObj { Name = "Test", Value = 42 });
			Assert.Contains("Test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void XmlDeserializer_Default_Culture()
		{
			var d = new XmlDeserializer();
			Assert.Equal(CultureInfo.InvariantCulture, d.Culture);
		}

		[Fact]
		public void XmlDeserializer_RootElement_Property()
		{
			var d = new XmlDeserializer();
			d.RootElement = "data";
			Assert.Equal("data", d.RootElement);
		}

		[Fact]
		public void XmlDeserializer_Namespace_Property()
		{
			var d = new XmlDeserializer();
			d.Namespace = "http://example.com/ns";
			Assert.Equal("http://example.com/ns", d.Namespace);
		}

		[Fact]
		public void XmlDeserializer_DateFormat_Property()
		{
			var d = new XmlDeserializer();
			d.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", d.DateFormat);
		}

		[Fact]
		public void XmlDeserializer_Deserialize_Simple()
		{
			var d = new XmlDeserializer();
			var response = new RestResponse { Content = "<SimpleObj><Name>Hello</Name><Value>99</Value></SimpleObj>" };
			var obj = d.Deserialize<SimpleObj>(response);
			Assert.Equal("Hello", obj.Name);
			Assert.Equal(99, obj.Value);
		}

		[Fact]
		public void XmlDeserializer_Deserialize_With_RootElement()
		{
			var d = new XmlDeserializer { RootElement = "Data" };
			var response = new RestResponse { Content = "<Root><Data><Name>Test</Name><Value>1</Value></Data></Root>" };
			var obj = d.Deserialize<SimpleObj>(response);
			Assert.Equal("Test", obj.Name);
		}

		[Fact]
		public void JsonDeserializer_Default_Culture()
		{
			var d = new JsonDeserializer();
			Assert.Equal(CultureInfo.InvariantCulture, d.Culture);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_With_RootElement()
		{
			var d = new JsonDeserializer { RootElement = "data" };
			var response = new RestResponse { Content = "{\"data\": {\"Name\": \"Test\", \"Value\": 42}}" };
			var obj = d.Deserialize<SimpleObj>(response);
			Assert.Equal("Test", obj.Name);
			Assert.Equal(42, obj.Value);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_List()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "[{\"Name\": \"A\", \"Value\": 1}, {\"Name\": \"B\", \"Value\": 2}]" };
			var list = d.Deserialize<List<SimpleObj>>(response);
			Assert.Equal(2, list.Count);
			Assert.Equal("A", list[0].Name);
			Assert.Equal("B", list[1].Name);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_List_With_RootElement()
		{
			var d = new JsonDeserializer { RootElement = "items" };
			var response = new RestResponse { Content = "{\"items\": [{\"Name\": \"A\"}, {\"Name\": \"B\"}]}" };
			var list = d.Deserialize<List<SimpleObj>>(response);
			Assert.Equal(2, list.Count);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Dictionary()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"key1\": \"val1\", \"key2\": \"val2\"}" };
			var dict = d.Deserialize<Dictionary<string, string>>(response);
			Assert.Equal(2, dict.Count);
			Assert.Equal("val1", dict["key1"]);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_With_DateFormat()
		{
			var d = new JsonDeserializer { DateFormat = "yyyy-MM-dd" };
			var response = new RestResponse { Content = "{\"Date\": \"2024-06-15\"}" };
			var obj = d.Deserialize<ObjWithDate>(response);
			Assert.Equal(2024, obj.Date.Year);
			Assert.Equal(6, obj.Date.Month);
			Assert.Equal(15, obj.Date.Day);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Nullable_Int()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"NullableValue\": 42}" };
			var obj = d.Deserialize<ObjWithNullable>(response);
			Assert.Equal(42, obj.NullableValue);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Null_Nullable_Int()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"NullableValue\": null}" };
			var obj = d.Deserialize<ObjWithNullable>(response);
			Assert.Null(obj.NullableValue);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Enum()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Status\": \"Completed\"}" };
			var obj = d.Deserialize<ObjWithEnum>(response);
			Assert.Equal(ResponseStatus.Completed, obj.Status);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Guid()
		{
			var d = new JsonDeserializer();
			var guid = Guid.NewGuid();
			var response = new RestResponse { Content = "{\"Id\": \"" + guid + "\"}" };
			var obj = d.Deserialize<ObjWithGuid>(response);
			Assert.Equal(guid, obj.Id);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_EmptyGuid()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Id\": \"\"}" };
			var obj = d.Deserialize<ObjWithGuid>(response);
			Assert.Equal(Guid.Empty, obj.Id);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Uri()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Link\": \"http://example.com/test\"}" };
			var obj = d.Deserialize<ObjWithUri>(response);
			Assert.Equal("http://example.com/test", obj.Link.ToString());
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Decimal()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Amount\": \"19.99\"}" };
			var obj = d.Deserialize<ObjWithDecimal>(response);
			Assert.Equal(19.99m, obj.Amount);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Nested_Object()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Inner\": {\"Name\": \"nested\", \"Value\": 1}}" };
			var obj = d.Deserialize<ObjWithNested>(response);
			Assert.NotNull(obj.Inner);
			Assert.Equal("nested", obj.Inner.Name);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Nested_List()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Items\": [{\"Name\": \"A\"}, {\"Name\": \"B\"}]}" };
			var obj = d.Deserialize<ObjWithList>(response);
			Assert.Equal(2, obj.Items.Count);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Nested_Dictionary()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Data\": {\"key1\": \"val1\", \"key2\": \"val2\"}}" };
			var obj = d.Deserialize<ObjWithDict>(response);
			Assert.Equal(2, obj.Data.Count);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Boolean_Property()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Active\": true}" };
			var obj = d.Deserialize<ObjWithBool>(response);
			Assert.True(obj.Active);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Long_Property()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"BigNumber\": 9999999999}" };
			var obj = d.Deserialize<ObjWithLong>(response);
			Assert.Equal(9999999999L, obj.BigNumber);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_Double_Property()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Ratio\": 3.14}" };
			var obj = d.Deserialize<ObjWithDouble>(response);
			Assert.Equal(3.14, obj.Ratio, 2);
		}

		[Fact]
		public void JsonDeserializer_Deserialize_DateTimeOffset()
		{
			var d = new JsonDeserializer();
			var response = new RestResponse { Content = "{\"Stamp\": \"2024-06-15T12:00:00Z\"}" };
			var obj = d.Deserialize<ObjWithDateTimeOffset>(response);
			Assert.Equal(2024, obj.Stamp.Year);
		}

		public class SimpleObj
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}

		public class ObjWithDate
		{
			public DateTime Date { get; set; }
		}

		public class ObjWithNullable
		{
			public int? NullableValue { get; set; }
		}

		public class ObjWithEnum
		{
			public ResponseStatus Status { get; set; }
		}

		public class ObjWithGuid
		{
			public Guid Id { get; set; }
		}

		public class ObjWithUri
		{
			public Uri Link { get; set; }
		}

		public class ObjWithDecimal
		{
			public decimal Amount { get; set; }
		}

		public class ObjWithNested
		{
			public SimpleObj Inner { get; set; }
		}

		public class ObjWithList
		{
			public List<SimpleObj> Items { get; set; }
		}

		public class ObjWithDict
		{
			public Dictionary<string, string> Data { get; set; }
		}

		public class ObjWithBool
		{
			public bool Active { get; set; }
		}

		public class ObjWithLong
		{
			public long BigNumber { get; set; }
		}

		public class ObjWithDouble
		{
			public double Ratio { get; set; }
		}

		public class ObjWithDateTimeOffset
		{
			public DateTimeOffset Stamp { get; set; }
		}
	}
}
