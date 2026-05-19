using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class TimeoutTests
	{
		private const string BaseUrl = "http://localhost:8896/";

		[Fact]
		public void Request_Level_Timeout_Triggers_Error()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.TimeoutHandler(5000)))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);
				request.Timeout = 500;

				var response = client.Execute(request);
				Assert.True(response.ResponseStatus == ResponseStatus.Error || response.ResponseStatus == ResponseStatus.TimedOut);
				Assert.NotNull(response.ErrorMessage);
			}
		}

		[Fact]
		public void Client_Level_Timeout_Triggers_Error()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.TimeoutHandler(5000)))
			{
				var client = new RestClient(BaseUrl);
				client.Timeout = 500;
				var request = new RestRequest("", Method.GET);

				var response = client.Execute(request);
				Assert.True(response.ResponseStatus == ResponseStatus.Error || response.ResponseStatus == ResponseStatus.TimedOut);
			}
		}

		[Fact]
		public void Request_Timeout_Overrides_Client_Timeout()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.TimeoutHandler(3000)))
			{
				var client = new RestClient(BaseUrl);
				client.Timeout = 10000;
				var request = new RestRequest("", Method.GET);
				request.Timeout = 500;

				var response = client.Execute(request);
				Assert.True(response.ResponseStatus == ResponseStatus.Error || response.ResponseStatus == ResponseStatus.TimedOut);
			}
		}
	}
}
