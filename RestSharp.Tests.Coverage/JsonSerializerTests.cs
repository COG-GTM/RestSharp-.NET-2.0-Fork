using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests
{
	public class JsonSerializerTests
	{
		[Fact]
		public void Default_Constructor_Sets_ContentType()
		{
			var serializer = new JsonSerializer();
			Assert.Equal("application/json", serializer.ContentType);
		}

		[Fact]
		public void Serialize_Simple_Object()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = "Test", Value = 42 });
			Assert.Contains("Test", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void Serialize_Null_Values_Included()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = (string)null, Value = 0 });
			Assert.Contains("null", result);
		}

		[Fact]
		public void Serialize_Nested_Object()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new
			{
				Person = new { Name = "John", Age = 30 },
				Items = new[] { "A", "B", "C" }
			});
			Assert.Contains("John", result);
			Assert.Contains("\"A\"", result);
		}

		[Fact]
		public void Custom_Serializer_Constructor()
		{
			var customSerializer = new Newtonsoft.Json.JsonSerializer
			{
				NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
			};
			var serializer = new JsonSerializer(customSerializer);
			Assert.Equal("application/json", serializer.ContentType);

			var result = serializer.Serialize(new { Name = (string)null, Value = 42 });
			Assert.DoesNotContain("Name", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void ContentType_Can_Be_Changed()
		{
			var serializer = new JsonSerializer();
			serializer.ContentType = "text/json";
			Assert.Equal("text/json", serializer.ContentType);
		}

		[Fact]
		public void DateFormat_Property()
		{
			var serializer = new JsonSerializer();
			serializer.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
		}

		[Fact]
		public void RootElement_Property()
		{
			var serializer = new JsonSerializer();
			serializer.RootElement = "data";
			Assert.Equal("data", serializer.RootElement);
		}

		[Fact]
		public void Namespace_Property()
		{
			var serializer = new JsonSerializer();
			serializer.Namespace = "ns";
			Assert.Equal("ns", serializer.Namespace);
		}
	}
}
