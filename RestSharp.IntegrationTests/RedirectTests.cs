using System.Net;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class RedirectTests
	{
		private const string BaseUrl = "http://localhost:8893/";

		[Fact]
		public void FollowRedirects_True_Follows_302()
		{
			// RedirectHandler needs multiple requests: 1 redirect + 1 final = 2
			using (SimpleServer.Create(BaseUrl, Handlers.RedirectHandler(1, BaseUrl), maxRequests: 2))
			{
				var client = new RestClient(BaseUrl);
				client.FollowRedirects = true;
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				Assert.Equal("Done", response.Content);
			}
		}

		[Fact]
		public void FollowRedirects_False_Does_Not_Follow()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.StatusCodeHandler(302)))
			{
				var client = new RestClient(BaseUrl);
				client.FollowRedirects = false;
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.Equal(HttpStatusCode.Found, response.StatusCode);
			}
		}
	}
}
