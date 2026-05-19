using System;
using System.Linq;
using System.Net;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class SyncRequestTests
	{
		private const string BaseUrl = "http://localhost:8888/";

		[Fact]
		public void POST_With_Form_Encoded_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddParameter("name", "test_value");

				var response = client.Execute(request);
				Assert.Contains("name=test_value", response.Content);
			}
		}

		[Fact]
		public void PUT_With_Form_Encoded_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.PUT);
				request.AddParameter("name", "test_value");

				var response = client.Execute(request);
				Assert.Contains("name=test_value", response.Content);
			}
		}

		[Fact]
		public void PATCH_With_Form_Encoded_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.PATCH);
				request.AddParameter("name", "test_value");

				var response = client.Execute(request);
				Assert.Contains("name=test_value", response.Content);
			}
		}

		[Fact]
		public void DELETE_Request()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoMethod))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.DELETE);

				var response = client.Execute(request);
				Assert.Equal("DELETE", response.Content);
			}
		}

		[Fact]
		public void HEAD_Request_Has_No_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoValue("should not appear")))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.HEAD);

				var response = client.Execute(request);
				Assert.True(string.IsNullOrEmpty(response.Content));
			}
		}

		[Fact]
		public void OPTIONS_Request()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoMethod))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.OPTIONS);

				var response = client.Execute(request);
				Assert.Equal("OPTIONS", response.Content);
			}
		}

		[Fact]
		public void POST_With_Json_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.RequestFormat = DataFormat.Json;
				request.AddBody(new { Name = "TestName", Value = 42 });

				var response = client.Execute(request);
				Assert.Contains("TestName", response.Content);
			}
		}

		[Fact]
		public void POST_With_Xml_Body()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.RequestFormat = DataFormat.Xml;
				request.AddBody(new XmlTestObject { Name = "TestName", Value = 42 });

				var response = client.Execute(request);
				Assert.Contains("TestName", response.Content);
			}
		}

		[Fact]
		public void Verify_Headers_Are_Sent()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoHeaders))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);
				request.AddHeader("X-Custom-Header", "custom_value");

				var response = client.Execute(request);
				Assert.Contains("X-Custom-Header", response.Content);
				Assert.Contains("custom_value", response.Content);
			}
		}

		[Fact]
		public void Verify_Cookies_Are_Sent()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoCookies))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);
				request.AddCookie("session", "abc123");

				var response = client.Execute(request);
				Assert.Contains("session=abc123", response.Content);
			}
		}

		[Fact]
		public void Verify_Url_Segments_Are_Substituted()
		{
			const string url = "http://localhost:8888/";
			using (SimpleServer.Create(url, Handlers.EchoValue("OK")))
			{
				var client = new RestClient(url);
				var request = new RestRequest("{resource}", Method.GET);
				request.AddUrlSegment("resource", "");

				var response = client.Execute(request);
				Assert.NotNull(response.Content);
			}
		}

		[Fact]
		public void GET_Returns_Status_200()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.EchoValue("OK")))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			}
		}

		public class XmlTestObject
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}
	}
}
