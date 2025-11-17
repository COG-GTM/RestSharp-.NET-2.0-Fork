using System;
using System.Threading;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class TimeoutTests
	{
		[Fact]
		public void RestClient_Handles_Request_Timeout_Synchronously()
		{
			const string baseUrl = "http://localhost:8080/";
			var resetEvent = new ManualResetEvent(false);

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 500;
				var request = new RestRequest("");

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);
			}
		}

		[Fact]
		public void RestClient_Handles_Request_Timeout_Asynchronously()
		{
			const string baseUrl = "http://localhost:8080/";
			var resetEvent = new ManualResetEvent(false);
			RestResponse capturedResponse = null;

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 500;
				var request = new RestRequest("");

				client.ExecuteAsync(request, response =>
				{
					capturedResponse = response;
					resetEvent.Set();
				});

				resetEvent.WaitOne(5000);

				Assert.NotNull(capturedResponse);
				Assert.Equal(ResponseStatus.TimedOut, capturedResponse.ResponseStatus);
			}
		}

		[Fact]
		public void RestClient_Respects_Request_Level_Timeout_Over_Client_Timeout()
		{
			const string baseUrl = "http://localhost:8080/";

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 5000;
				var request = new RestRequest("");
				request.Timeout = 500;

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);
			}
		}

		[Fact]
		public void RestClient_Returns_Error_Message_On_Timeout()
		{
			const string baseUrl = "http://localhost:8080/";

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 500;
				var request = new RestRequest("");

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);
				Assert.NotNull(response.ErrorException);
			}
		}

		[Fact]
		public void RestClient_Handles_Zero_Timeout_As_No_Timeout()
		{
			const string baseUrl = "http://localhost:8080/";
			const string val = "No timeout test";

			using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 0;
				var request = new RestRequest("");

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
				Assert.Equal(val, response.Content);
			}
		}

		[Fact]
		public void RestClient_Completes_Request_Before_Timeout()
		{
			const string baseUrl = "http://localhost:8080/";
			const string val = "Fast response";

			using (SimpleServer.Create(baseUrl, Handlers.EchoValue(val)))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 5000;
				var request = new RestRequest("");

				var response = client.Execute(request);

				Assert.Equal(ResponseStatus.Completed, response.ResponseStatus);
				Assert.Equal(val, response.Content);
			}
		}

		[Fact]
		public void RestClient_Handles_Timeout_With_Generic_Execute()
		{
			const string baseUrl = "http://localhost:8080/";

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 500;
				var request = new RestRequest("");

				var response = client.Execute<TestResponse>(request);

				Assert.Equal(ResponseStatus.TimedOut, response.ResponseStatus);
				Assert.Null(response.Data);
			}
		}

		[Fact]
		public void RestClient_Handles_Timeout_Async_With_Generic_Execute()
		{
			const string baseUrl = "http://localhost:8080/";
			var resetEvent = new ManualResetEvent(false);
			RestResponse<TestResponse> capturedResponse = null;

			using (SimpleServer.Create(baseUrl, Handlers.Timeout()))
			{
				var client = new RestClient(baseUrl);
				client.Timeout = 500;
				var request = new RestRequest("");

				client.ExecuteAsync<TestResponse>(request, response =>
				{
					capturedResponse = response;
					resetEvent.Set();
				});

				resetEvent.WaitOne(5000);

				Assert.NotNull(capturedResponse);
				Assert.Equal(ResponseStatus.TimedOut, capturedResponse.ResponseStatus);
				Assert.Null(capturedResponse.Data);
			}
		}

		public class TestResponse
		{
			public string Message { get; set; }
		}
	}
}
