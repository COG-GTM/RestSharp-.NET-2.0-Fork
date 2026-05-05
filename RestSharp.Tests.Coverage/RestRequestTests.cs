using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests
{
	public class RestRequestTests
	{
		[Fact]
		public void Default_Constructor_Sets_Defaults()
		{
			var request = new RestRequest();
			Assert.NotNull(request.Parameters);
			Assert.Empty(request.Parameters);
			Assert.NotNull(request.Files);
			Assert.Empty(request.Files);
			Assert.Equal(Method.GET, request.Method);
			Assert.Equal(DataFormat.Xml, request.RequestFormat);
			Assert.NotNull(request.JsonSerializer);
			Assert.NotNull(request.XmlSerializer);
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
			var request = new RestRequest("resource/foo");
			Assert.Equal("resource/foo", request.Resource);
			Assert.Equal(Method.GET, request.Method);
		}

		[Fact]
		public void Constructor_With_Resource_And_Method()
		{
			var request = new RestRequest("resource/foo", Method.PUT);
			Assert.Equal("resource/foo", request.Resource);
			Assert.Equal(Method.PUT, request.Method);
		}

		[Fact]
		public void Constructor_With_Uri_Sets_Resource()
		{
			var uri = new Uri("/api/resource", UriKind.Relative);
			var request = new RestRequest(uri);
			Assert.Equal("/api/resource", request.Resource);
		}

		[Fact]
		public void Constructor_With_Absolute_Uri_Uses_AbsolutePath_And_Query()
		{
			var uri = new Uri("http://example.com/api/resource?key=val");
			var request = new RestRequest(uri, Method.POST);
			Assert.Equal("/api/resource?key=val", request.Resource);
			Assert.Equal(Method.POST, request.Method);
		}

		[Fact]
		public void AddParameter_Adds_GetOrPost_Parameter()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value");
			Assert.Single(request.Parameters);
			Assert.Equal("name", request.Parameters[0].Name);
			Assert.Equal("value", request.Parameters[0].Value);
			Assert.Equal(ParameterType.GetOrPost, request.Parameters[0].Type);
		}

		[Fact]
		public void AddParameter_With_Type()
		{
			var request = new RestRequest();
			request.AddParameter("Authorization", "Bearer token", ParameterType.HttpHeader);
			Assert.Single(request.Parameters);
			Assert.Equal(ParameterType.HttpHeader, request.Parameters[0].Type);
		}

		[Fact]
		public void AddParameter_With_Parameter_Object()
		{
			var request = new RestRequest();
			var param = new Parameter { Name = "test", Value = "val", Type = ParameterType.Cookie };
			request.AddParameter(param);
			Assert.Single(request.Parameters);
			Assert.Equal(ParameterType.Cookie, request.Parameters[0].Type);
		}

		[Fact]
		public void AddHeader_Adds_HttpHeader_Parameter()
		{
			var request = new RestRequest();
			request.AddHeader("X-Custom", "value");
			Assert.Single(request.Parameters);
			Assert.Equal(ParameterType.HttpHeader, request.Parameters[0].Type);
			Assert.Equal("X-Custom", request.Parameters[0].Name);
		}

		[Fact]
		public void AddCookie_Adds_Cookie_Parameter()
		{
			var request = new RestRequest();
			request.AddCookie("session", "abc123");
			Assert.Single(request.Parameters);
			Assert.Equal(ParameterType.Cookie, request.Parameters[0].Type);
			Assert.Equal("session", request.Parameters[0].Name);
		}

		[Fact]
		public void AddUrlSegment_Adds_UrlSegment_Parameter()
		{
			var request = new RestRequest();
			request.AddUrlSegment("id", "42");
			Assert.Single(request.Parameters);
			Assert.Equal(ParameterType.UrlSegment, request.Parameters[0].Type);
			Assert.Equal("id", request.Parameters[0].Name);
		}

		[Fact]
		public void AddBody_Json_Format()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "Test", Value = 123 });
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
			Assert.Contains("Test", bodyParam.Value.ToString());
		}

		[Fact]
		public void AddBody_Xml_Format()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestPoco { Name = "Foo", Age = 25 });
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
			Assert.Contains("Foo", bodyParam.Value.ToString());
		}

		[Fact]
		public void AddBody_With_XmlNamespace()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestPoco { Name = "Bar" }, "http://example.com/ns");
			var bodyParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(bodyParam);
		}

		[Fact]
		public void AddObject_Adds_Parameters_For_All_Properties()
		{
			var request = new RestRequest();
			var obj = new TestPoco { Name = "Test", Age = 30 };
			request.AddObject(obj);
			Assert.Equal(2, request.Parameters.Count);
			Assert.Contains(request.Parameters, p => p.Name == "Name" && p.Value.ToString() == "Test");
			Assert.Contains(request.Parameters, p => p.Name == "Age" && p.Value.ToString() == "30");
		}

		[Fact]
		public void AddObject_With_Whitelist()
		{
			var request = new RestRequest();
			var obj = new TestPoco { Name = "Test", Age = 30 };
			request.AddObject(obj, "Name");
			Assert.Single(request.Parameters);
			Assert.Equal("Name", request.Parameters[0].Name);
		}

		[Fact]
		public void AddObject_Skips_Null_Values()
		{
			var request = new RestRequest();
			var obj = new TestPoco { Name = null, Age = 30 };
			request.AddObject(obj);
			Assert.Single(request.Parameters);
			Assert.Equal("Age", request.Parameters[0].Name);
		}

		[Fact]
		public void AddFile_With_Bytes()
		{
			var request = new RestRequest();
			var bytes = new byte[] { 1, 2, 3, 4, 5 };
			request.AddFile("file", bytes, "test.txt");
			Assert.Single(request.Files);
			Assert.Equal("file", request.Files[0].Name);
			Assert.Equal("test.txt", request.Files[0].FileName);
		}

		[Fact]
		public void AddFile_With_Bytes_And_ContentType()
		{
			var request = new RestRequest();
			var bytes = new byte[] { 1, 2, 3 };
			request.AddFile("file", bytes, "test.pdf", "application/pdf");
			Assert.Single(request.Files);
			Assert.Equal("application/pdf", request.Files[0].ContentType);
		}

		[Fact]
		public void AddFile_With_Writer()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => { };
			request.AddFile("doc", writer, "doc.txt");
			Assert.Single(request.Files);
			Assert.Equal("doc", request.Files[0].Name);
			Assert.Equal("doc.txt", request.Files[0].FileName);
		}

		[Fact]
		public void AddFile_With_Writer_And_ContentType()
		{
			var request = new RestRequest();
			Action<Stream> writer = s => { };
			request.AddFile("doc", writer, "doc.xml", "application/xml");
			Assert.Single(request.Files);
			Assert.Equal("application/xml", request.Files[0].ContentType);
		}

		[Fact]
		public void Method_Property_Defaults_To_GET()
		{
			var request = new RestRequest();
			Assert.Equal(Method.GET, request.Method);
		}

		[Fact]
		public void Method_Property_Can_Be_Set()
		{
			var request = new RestRequest();
			request.Method = Method.DELETE;
			Assert.Equal(Method.DELETE, request.Method);
		}

		[Fact]
		public void RequestFormat_Defaults_To_Xml()
		{
			var request = new RestRequest();
			Assert.Equal(DataFormat.Xml, request.RequestFormat);
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
		public void OnBeforeDeserialization_Defaults_To_NoOp()
		{
			var request = new RestRequest();
			Assert.NotNull(request.OnBeforeDeserialization);
			// should not throw
			request.OnBeforeDeserialization(new RestResponse());
		}

		[Fact]
		public void Timeout_Can_Be_Set()
		{
			var request = new RestRequest();
			request.Timeout = 5000;
			Assert.Equal(5000, request.Timeout);
		}

		[Fact]
		public void RootElement_Can_Be_Set()
		{
			var request = new RestRequest();
			request.RootElement = "data";
			Assert.Equal("data", request.RootElement);
		}

		[Fact]
		public void DateFormat_Can_Be_Set()
		{
			var request = new RestRequest();
			request.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", request.DateFormat);
		}

		[Fact]
		public void XmlNamespace_Can_Be_Set()
		{
			var request = new RestRequest();
			request.XmlNamespace = "http://example.com/ns";
			Assert.Equal("http://example.com/ns", request.XmlNamespace);
		}

		[Fact]
		public void Credentials_Can_Be_Set()
		{
			var request = new RestRequest();
			var creds = new System.Net.NetworkCredential("user", "pass");
			request.Credentials = creds;
			Assert.Equal(creds, request.Credentials);
		}

		[Fact]
		public void UserState_Can_Be_Set()
		{
			var request = new RestRequest();
			request.UserState = "custom state";
			Assert.Equal("custom state", request.UserState);
		}

		[Fact]
		public void AddParameter_Returns_This()
		{
			var request = new RestRequest();
			var result = request.AddParameter("name", "value");
			Assert.Same(request, result);
		}

		[Fact]
		public void AddHeader_Returns_This()
		{
			var request = new RestRequest();
			var result = request.AddHeader("name", "value");
			Assert.Same(request, result);
		}

		[Fact]
		public void AddCookie_Returns_This()
		{
			var request = new RestRequest();
			var result = request.AddCookie("name", "value");
			Assert.Same(request, result);
		}

		[Fact]
		public void AddUrlSegment_Returns_This()
		{
			var request = new RestRequest();
			var result = request.AddUrlSegment("name", "value");
			Assert.Same(request, result);
		}

		[Fact]
		public void AddObject_Returns_This()
		{
			var request = new RestRequest();
			var result = request.AddObject(new TestPoco { Name = "x" });
			Assert.Same(request, result);
		}

		[Fact]
		public void AddObject_With_Array_Property()
		{
			var request = new RestRequest();
			var obj = new TestPocoWithArray { Tags = new[] { "a", "b", "c" } };
			request.AddObject(obj);
			var param = request.Parameters.FirstOrDefault(p => p.Name == "Tags");
			Assert.NotNull(param);
			Assert.Equal("a,b,c", param.Value);
		}

		private class TestPoco
		{
			public string Name { get; set; }
			public int Age { get; set; }
		}

		private class TestPocoWithArray
		{
			public string[] Tags { get; set; }
		}
	}
}
