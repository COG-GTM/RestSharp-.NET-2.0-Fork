using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests
{
	public class JsonSerializerTests
	{
		[Fact]
		public void Serializes_Simple_Object_To_Json()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = "Test", Value = 42 });

			Assert.Contains("\"Name\"", result);
			Assert.Contains("\"Test\"", result);
			Assert.Contains("42", result);
		}

		[Fact]
		public void Handles_Null_Values()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = (string)null, Value = 42 });

			Assert.Contains("null", result);
		}

		[Fact]
		public void Handles_Default_Values()
		{
			var serializer = new JsonSerializer();
			var result = serializer.Serialize(new { Name = "", Count = 0 });

			Assert.Contains("\"Name\"", result);
			Assert.Contains("\"Count\"", result);
		}

		[Fact]
		public void ContentType_Is_Application_Json()
		{
			var serializer = new JsonSerializer();
			Assert.Equal("application/json", serializer.ContentType);
		}

		[Fact]
		public void Properties_Exist()
		{
			var serializer = new JsonSerializer();
			serializer.RootElement = "root";
			serializer.Namespace = "ns";
			serializer.DateFormat = "yyyy-MM-dd";

			Assert.Equal("root", serializer.RootElement);
			Assert.Equal("ns", serializer.Namespace);
			Assert.Equal("yyyy-MM-dd", serializer.DateFormat);
		}

		[Fact]
		public void Custom_JsonNet_Serializer()
		{
			var newtonsoftSerializer = new Newtonsoft.Json.JsonSerializer
			{
				NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
			};
			var serializer = new JsonSerializer(newtonsoftSerializer);

			var result = serializer.Serialize(new { Name = (string)null, Value = 42 });
			Assert.DoesNotContain("Name", result);
		}
	}
}
