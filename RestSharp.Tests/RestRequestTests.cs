using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RestSharp.Tests
{
	public class RestRequestTests
	{
		[Fact]
		public void Default_Constructor_Sets_Method_To_GET()
		{
			var request = new RestRequest();
			Assert.Equal(Method.GET, request.Method);
		}

		[Fact]
		public void Default_Constructor_Sets_RequestFormat_To_Xml()
		{
			var request = new RestRequest();
			Assert.Equal(DataFormat.Xml, request.RequestFormat);
		}

		[Fact]
		public void Constructor_With_Method_Sets_Method()
		{
			var request = new RestRequest(Method.POST);
			Assert.Equal(Method.POST, request.Method);
		}

		[Fact]
		public void Constructor_With_Resource_Sets_Resource()
		{
			var request = new RestRequest("resource/item");
			Assert.Equal("resource/item", request.Resource);
		}

		[Fact]
		public void Constructor_With_Resource_Defaults_To_GET()
		{
			var request = new RestRequest("resource");
			Assert.Equal(Method.GET, request.Method);
		}

		[Fact]
		public void Constructor_With_Resource_And_Method()
		{
			var request = new RestRequest("resource", Method.PUT);
			Assert.Equal("resource", request.Resource);
			Assert.Equal(Method.PUT, request.Method);
		}

		[Fact]
		public void Constructor_With_Uri_Sets_Resource()
		{
			var uri = new Uri("/resource/item", UriKind.Relative);
			var request = new RestRequest(uri);
			Assert.Equal("/resource/item", request.Resource);
		}

		[Fact]
		public void Constructor_With_Absolute_Uri_Sets_Resource()
		{
			var uri = new Uri("http://example.com/resource/item");
			var request = new RestRequest(uri);
			Assert.Equal("/resource/item", request.Resource);
		}

		[Fact]
		public void Constructor_With_Uri_And_Method()
		{
			var uri = new Uri("/resource", UriKind.Relative);
			var request = new RestRequest(uri, Method.DELETE);
			Assert.Equal("/resource", request.Resource);
			Assert.Equal(Method.DELETE, request.Method);
		}

		[Fact]
		public void AddParameter_Adds_GetOrPost_By_Default()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value");
			var param = request.Parameters.First();
			Assert.Equal("name", param.Name);
			Assert.Equal("value", param.Value);
			Assert.Equal(ParameterType.GetOrPost, param.Type);
		}

		[Fact]
		public void AddParameter_With_Type_Sets_Correct_Type()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value", ParameterType.HttpHeader);
			Assert.Equal(ParameterType.HttpHeader, request.Parameters.First().Type);
		}

		[Fact]
		public void AddParameter_With_Cookie_Type()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value", ParameterType.Cookie);
			Assert.Equal(ParameterType.Cookie, request.Parameters.First().Type);
		}

		[Fact]
		public void AddParameter_With_UrlSegment_Type()
		{
			var request = new RestRequest();
			request.AddParameter("id", "123", ParameterType.UrlSegment);
			Assert.Equal(ParameterType.UrlSegment, request.Parameters.First().Type);
		}

		[Fact]
		public void AddParameter_With_RequestBody_Type()
		{
			var request = new RestRequest();
			request.AddParameter("application/json", "{}", ParameterType.RequestBody);
			Assert.Equal(ParameterType.RequestBody, request.Parameters.First().Type);
		}

		[Fact]
		public void AddHeader_Adds_HttpHeader_Parameter()
		{
			var request = new RestRequest();
			request.AddHeader("X-Custom", "test");
			var param = request.Parameters.First();
			Assert.Equal("X-Custom", param.Name);
			Assert.Equal("test", param.Value);
			Assert.Equal(ParameterType.HttpHeader, param.Type);
		}

		[Fact]
		public void AddCookie_Adds_Cookie_Parameter()
		{
			var request = new RestRequest();
			request.AddCookie("session", "abc123");
			var param = request.Parameters.First();
			Assert.Equal("session", param.Name);
			Assert.Equal("abc123", param.Value);
			Assert.Equal(ParameterType.Cookie, param.Type);
		}

		[Fact]
		public void AddUrlSegment_Adds_UrlSegment_Parameter()
		{
			var request = new RestRequest();
			request.AddUrlSegment("id", "42");
			var param = request.Parameters.First();
			Assert.Equal("id", param.Name);
			Assert.Equal("42", param.Value);
			Assert.Equal(ParameterType.UrlSegment, param.Type);
		}

		[Fact]
		public void AddBody_With_Json_Format_Serializes_To_Json()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "Test", Value = 42 });

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
			Assert.Contains("Test", body.Value.ToString());
			Assert.Equal("application/json", body.Name);
		}

		[Fact]
		public void AddBody_With_Xml_Format_Serializes_To_Xml()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestObject { Name = "Test", Value = 42 });

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
			Assert.Contains("Test", body.Value.ToString());
			Assert.Equal("text/xml", body.Name);
		}

		[Fact]
		public void AddBody_With_XmlNamespace()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestObject { Name = "Test" }, "http://example.com/ns");

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
		}

		[Fact]
		public void AddFile_With_Path_Adds_To_Files()
		{
			var request = new RestRequest();
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "test content");

			try
			{
				request.AddFile("file", tempFile);
				Assert.Equal(1, request.Files.Count);
				Assert.Equal("file", request.Files[0].Name);
				Assert.Equal(Path.GetFileName(tempFile), request.Files[0].FileName);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[Fact]
		public void AddFile_With_Bytes_Adds_To_Files()
		{
			var request = new RestRequest();
			var bytes = new byte[] { 1, 2, 3 };
			request.AddFile("file", bytes, "test.bin");
			Assert.Equal(1, request.Files.Count);
			Assert.Equal("file", request.Files[0].Name);
			Assert.Equal("test.bin", request.Files[0].FileName);
		}

		[Fact]
		public void AddFile_With_Bytes_And_ContentType()
		{
			var request = new RestRequest();
			var bytes = new byte[] { 1, 2, 3 };
			request.AddFile("file", bytes, "test.bin", "application/octet-stream");
			Assert.Equal("application/octet-stream", request.Files[0].ContentType);
		}

		[Fact]
		public void AddFile_With_Writer_Adds_To_Files()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => s.Write(new byte[] { 1, 2, 3 }, 0, 3);
			request.AddFile("file", writer, "test.bin");
			Assert.Equal(1, request.Files.Count);
			Assert.Equal("file", request.Files[0].Name);
			Assert.Equal("test.bin", request.Files[0].FileName);
		}

		[Fact]
		public void AddFile_With_Writer_And_ContentType()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => s.Write(new byte[] { 1, 2, 3 }, 0, 3);
			request.AddFile("file", writer, "test.bin", "image/png");
			Assert.Equal("image/png", request.Files[0].ContentType);
		}

		[Fact]
		public void AddObject_Adds_All_Public_Properties()
		{
			var request = new RestRequest();
			request.AddObject(new { Name = "Test", Age = 25, City = "NY" });

			Assert.Equal(3, request.Parameters.Count);
			Assert.True(request.Parameters.Any(p => p.Name == "Name" && (string)p.Value == "Test"));
			Assert.True(request.Parameters.Any(p => p.Name == "Age" && (int)p.Value == 25));
			Assert.True(request.Parameters.Any(p => p.Name == "City" && (string)p.Value == "NY"));
		}

		[Fact]
		public void AddObject_With_Whitelist_Only_Adds_Whitelisted()
		{
			var request = new RestRequest();
			request.AddObject(new { Name = "Test", Age = 25, City = "NY" }, "Name", "City");

			Assert.Equal(2, request.Parameters.Count);
			Assert.True(request.Parameters.Any(p => p.Name == "Name"));
			Assert.True(request.Parameters.Any(p => p.Name == "City"));
			Assert.False(request.Parameters.Any(p => p.Name == "Age"));
		}

		[Fact]
		public void IncreaseNumAttempts_Increments_Attempts()
		{
			var request = new RestRequest();
			Assert.Equal(0, request.Attempts);

			request.IncreaseNumAttempts();
			Assert.Equal(1, request.Attempts);

			request.IncreaseNumAttempts();
			Assert.Equal(2, request.Attempts);
		}

		[Fact]
		public void Parameters_Collection_Initialized_Empty()
		{
			var request = new RestRequest();
			Assert.NotNull(request.Parameters);
			Assert.Equal(0, request.Parameters.Count);
		}

		[Fact]
		public void Files_Collection_Initialized_Empty()
		{
			var request = new RestRequest();
			Assert.NotNull(request.Files);
			Assert.Equal(0, request.Files.Count);
		}

		public class TestObject
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
	}
}
