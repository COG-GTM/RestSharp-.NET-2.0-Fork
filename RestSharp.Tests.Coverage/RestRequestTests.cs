using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;
using RestSharp.Serializers;
using Xunit;

namespace RestSharp.Tests.Coverage
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
		public void Constructor_With_Method_Sets_Method()
		{
			var request = new RestRequest(Method.POST);
			Assert.Equal(Method.POST, request.Method);
		}

		[Fact]
		public void Constructor_With_Resource_Sets_Resource()
		{
			var request = new RestRequest("resource/path");
			Assert.Equal("resource/path", request.Resource);
		}

		[Fact]
		public void Constructor_With_Resource_And_Method_Sets_Both()
		{
			var request = new RestRequest("resource/path", Method.PUT);
			Assert.Equal("resource/path", request.Resource);
			Assert.Equal(Method.PUT, request.Method);
		}

		[Fact]
		public void Constructor_With_Uri_Sets_Resource()
		{
			var uri = new Uri("resource/path", UriKind.Relative);
			var request = new RestRequest(uri);
			Assert.Equal("resource/path", request.Resource);
		}

		[Fact]
		public void Constructor_With_Uri_And_Method_Sets_Both()
		{
			var uri = new Uri("resource/path", UriKind.Relative);
			var request = new RestRequest(uri, Method.DELETE);
			Assert.Equal("resource/path", request.Resource);
			Assert.Equal(Method.DELETE, request.Method);
		}

		[Fact]
		public void Default_Request_Format_Is_Xml()
		{
			var request = new RestRequest();
			Assert.Equal(DataFormat.Xml, request.RequestFormat);
		}

		[Fact]
		public void AddParameter_String_Name_Value_Adds_GetOrPost()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value");
			var param = request.Parameters.First();

			Assert.Equal("name", param.Name);
			Assert.Equal("value", param.Value);
			Assert.Equal(ParameterType.GetOrPost, param.Type);
		}

		[Fact]
		public void AddParameter_With_Type_Adds_Correct_Type()
		{
			var request = new RestRequest();
			request.AddParameter("name", "value", ParameterType.HttpHeader);
			var param = request.Parameters.First();

			Assert.Equal(ParameterType.HttpHeader, param.Type);
		}

		[Fact]
		public void AddParameter_Object_Adds_To_Collection()
		{
			var request = new RestRequest();
			var p = new Parameter { Name = "test", Value = "val", Type = ParameterType.Cookie };
			request.AddParameter(p);

			Assert.Contains(p, request.Parameters);
		}

		[Fact]
		public void AddHeader_Adds_HttpHeader_Parameter()
		{
			var request = new RestRequest();
			request.AddHeader("Content-Type", "application/json");

			var param = request.Parameters.First();
			Assert.Equal("Content-Type", param.Name);
			Assert.Equal("application/json", param.Value);
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
			var request = new RestRequest("users/{id}");
			request.AddUrlSegment("id", "42");

			var param = request.Parameters.First();
			Assert.Equal("id", param.Name);
			Assert.Equal("42", param.Value);
			Assert.Equal(ParameterType.UrlSegment, param.Type);
		}

		[Fact]
		public void AddBody_Json_Adds_RequestBody_Parameter()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Json;
			request.AddBody(new { Name = "test", Value = 42 });

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
			Assert.Contains("test", body.Value.ToString());
		}

		[Fact]
		public void AddBody_Xml_Adds_RequestBody_Parameter()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestObj { Name = "test" });

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
			Assert.Contains("test", body.Value.ToString());
		}

		[Fact]
		public void AddBody_With_Namespace_Sets_Xml_Namespace()
		{
			var request = new RestRequest();
			request.RequestFormat = DataFormat.Xml;
			request.AddBody(new TestObj { Name = "test" }, "http://example.com/ns");

			var body = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.RequestBody);
			Assert.NotNull(body);
		}

		[Fact]
		public void AddObject_Adds_Public_Properties_As_Parameters()
		{
			var request = new RestRequest();
			var obj = new { Name = "John", Age = 30 };
			request.AddObject(obj);

			Assert.Equal(2, request.Parameters.Count);
			Assert.Equal("Name", request.Parameters[0].Name);
			Assert.Equal("John", request.Parameters[0].Value);
			Assert.Equal("Age", request.Parameters[1].Name);
			Assert.Equal(30, request.Parameters[1].Value);
		}

		[Fact]
		public void AddObject_With_Whitelist_Only_Adds_Listed_Properties()
		{
			var request = new RestRequest();
			var obj = new TestObj { Name = "John", Value = 30 };
			request.AddObject(obj, "Name");

			Assert.Single(request.Parameters);
			Assert.Equal("Name", request.Parameters[0].Name);
		}

		[Fact]
		public void AddObject_Skips_Null_Values()
		{
			var request = new RestRequest();
			var obj = new TestObj { Name = null, Value = 42 };
			request.AddObject(obj);

			Assert.DoesNotContain(request.Parameters, p => p.Name == "Name");
			Assert.Contains(request.Parameters, p => p.Name == "Value");
		}

		[Fact]
		public void AddFile_From_Bytes_Adds_To_Files_Collection()
		{
			var request = new RestRequest();
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.txt");

			Assert.Single(request.Files);
			Assert.Equal("file", request.Files[0].Name);
			Assert.Equal("test.txt", request.Files[0].FileName);
		}

		[Fact]
		public void AddFile_From_Bytes_With_ContentType_Sets_ContentType()
		{
			var request = new RestRequest();
			request.AddFile("file", new byte[] { 1, 2, 3 }, "test.txt", "text/plain");

			Assert.Equal("text/plain", request.Files[0].ContentType);
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
		public void Request_Has_Empty_Parameters_By_Default()
		{
			var request = new RestRequest();
			Assert.NotNull(request.Parameters);
			Assert.Empty(request.Parameters);
		}

		[Fact]
		public void Request_Has_Empty_Files_By_Default()
		{
			var request = new RestRequest();
			Assert.NotNull(request.Files);
			Assert.Empty(request.Files);
		}

		[Fact]
		public void OnBeforeDeserialization_Can_Be_Set_And_Invoked()
		{
			var request = new RestRequest();
			bool callbackInvoked = false;
			request.OnBeforeDeserialization = r => { callbackInvoked = true; };

			var response = new RestResponse { Content = "test" };
			request.OnBeforeDeserialization(response);

			Assert.True(callbackInvoked);
		}

		[Fact]
		public void Request_Timeout_Can_Be_Set()
		{
			var request = new RestRequest();
			request.Timeout = 5000;
			Assert.Equal(5000, request.Timeout);
		}

		[Fact]
		public void Request_DateFormat_Can_Be_Set()
		{
			var request = new RestRequest();
			request.DateFormat = "yyyy-MM-dd";
			Assert.Equal("yyyy-MM-dd", request.DateFormat);
		}

		[Fact]
		public void Request_XmlNamespace_Can_Be_Set()
		{
			var request = new RestRequest();
			request.XmlNamespace = "http://example.com";
			Assert.Equal("http://example.com", request.XmlNamespace);
		}

		[Fact]
		public void Request_RootElement_Can_Be_Set()
		{
			var request = new RestRequest();
			request.RootElement = "data";
			Assert.Equal("data", request.RootElement);
		}

		[Fact]
		public void Request_Credentials_Can_Be_Set()
		{
			var request = new RestRequest();
			var creds = new System.Net.NetworkCredential("user", "pass");
			request.Credentials = creds;
			Assert.Equal(creds, request.Credentials);
		}

		[Fact]
		public void Request_JsonSerializer_Has_Default()
		{
			var request = new RestRequest();
			Assert.NotNull(request.JsonSerializer);
		}

		[Fact]
		public void Request_XmlSerializer_Has_Default()
		{
			var request = new RestRequest();
			Assert.NotNull(request.XmlSerializer);
		}

		[Fact]
		public void AddObject_With_Array_Property_Joins_Values()
		{
			var request = new RestRequest();
			var obj = new ObjectWithArray { Tags = new[] { "a", "b", "c" } };
			request.AddObject(obj);

			var param = request.Parameters.First(p => p.Name == "Tags");
			Assert.Equal("a,b,c", param.Value);
		}

		private class TestObj
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}

		private class ObjectWithArray
		{
			public string[] Tags { get; set; }
		}
	}
}
