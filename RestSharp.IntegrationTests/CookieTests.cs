using System;
using System.Net;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class CookieTests
	{
		private const string BaseUrl = "http://localhost:8892/";

		[Fact]
		public void Send_Cookies_With_Request()
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
		public void Receive_Set_Cookie_From_Server()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.SetCookieHandler("test_cookie", "test_value")))
			{
				var client = new RestClient(BaseUrl);
				client.CookieContainer = new CookieContainer();
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.True(response.Cookies.Count > 0 || client.CookieContainer.Count > 0);
			}
		}

		[Fact]
		public void CookieContainer_Is_Shared_Across_Requests()
		{
			var container = new CookieContainer();
			container.Add(new Uri(BaseUrl), new Cookie("shared", "cookie_value"));

			using (SimpleServer.Create(BaseUrl, Handlers.EchoCookies))
			{
				var client = new RestClient(BaseUrl);
				client.CookieContainer = container;
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.Contains("shared=cookie_value", response.Content);
			}
		}
	}
}
