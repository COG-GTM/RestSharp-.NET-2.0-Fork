using System.Threading;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class AsyncTests
	{
		[Fact]
		public void Can_Perform_GET_Async()
		{
			var baseUrl = TestServerUrl.Next();
			const string val = "Basic async test";

			using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
			{
				var client = new RestClient(baseUrl);
				var request = new RestRequest("");

				var response = client.ExecuteAsync(request).GetAwaiter().GetResult();

				Assert.NotNull(response.Content);
				Assert.Equal(val, response.Content);
			}
		}

		[Fact]
		public void Can_Cancel_GET_Async()
		{
			var baseUrl = TestServerUrl.Next();

			using (SimpleServer.Create(baseUrl, Handlers.EchoValue("never read")))
			{
				var client = new RestClient(baseUrl);
				var request = new RestRequest("");

				var cancelled = new CancellationTokenSource();
				cancelled.Cancel();

				var response = client.ExecuteAsync(request, cancelled.Token).GetAwaiter().GetResult();

				Assert.Equal(ResponseStatus.Aborted, response.ResponseStatus);
			}
		}
	}
}
