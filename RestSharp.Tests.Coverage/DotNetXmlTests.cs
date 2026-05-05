using System;
using System.Text;
using System.Xml.Serialization;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests
{
	public class DotNetXmlTests
	{
		[Fact]
		public void DotNetXmlSerializer_Default_ContentType()
		{
			var s = new DotNetXmlSerializer();
			Assert.Equal("application/xml", s.ContentType);
		}

		[Fact]
		public void DotNetXmlSerializer_Default_Encoding()
		{
			var s = new DotNetXmlSerializer();
			Assert.Equal(Encoding.UTF8, s.Encoding);
		}

		[Fact]
		public void DotNetXmlSerializer_Constructor_With_Namespace()
		{
			var s = new DotNetXmlSerializer("http://example.com/ns");
			Assert.Equal("http://example.com/ns", s.Namespace);
		}

		[Fact]
		public void DotNetXmlSerializer_Serialize()
		{
			var s = new DotNetXmlSerializer();
			var result = s.Serialize(new DotNetTestObj { Name = "Test", Value = 42 });
			Assert.Contains("Test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void DotNetXmlSerializer_Properties()
		{
			var s = new DotNetXmlSerializer();
			s.RootElement = "Root";
			s.DateFormat = "yyyy-MM-dd";
			s.ContentType = "text/xml";
			Assert.Equal("Root", s.RootElement);
			Assert.Equal("yyyy-MM-dd", s.DateFormat);
			Assert.Equal("text/xml", s.ContentType);
		}

		[Fact]
		public void DotNetXmlDeserializer_Deserialize()
		{
			var d = new DotNetXmlDeserializer();
			var xml = "<?xml version=\"1.0\"?><DotNetTestObj xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Name>Hello</Name><Value>99</Value></DotNetTestObj>";
			var response = new RestResponse { Content = xml };
			var obj = d.Deserialize<DotNetTestObj>(response);
			Assert.Equal("Hello", obj.Name);
			Assert.Equal(99, obj.Value);
		}

		[Fact]
		public void DotNetXmlDeserializer_Deserialize_Empty_Content()
		{
			var d = new DotNetXmlDeserializer();
			var response = new RestResponse { Content = "" };
			var obj = d.Deserialize<DotNetTestObj>(response);
			Assert.Null(obj);
		}

		[Fact]
		public void DotNetXmlDeserializer_Deserialize_Null_Content()
		{
			var d = new DotNetXmlDeserializer();
			var response = new RestResponse { Content = null };
			var obj = d.Deserialize<DotNetTestObj>(response);
			Assert.Null(obj);
		}

		[Fact]
		public void DotNetXmlDeserializer_Properties()
		{
			var d = new DotNetXmlDeserializer();
			d.DateFormat = "yyyy-MM-dd";
			d.Namespace = "http://ns";
			d.RootElement = "Root";
			Assert.Equal("yyyy-MM-dd", d.DateFormat);
			Assert.Equal("http://ns", d.Namespace);
			Assert.Equal("Root", d.RootElement);
		}
	}

	public class DotNetTestObj
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}
}
