using System;
using System.Net;
using System.Threading;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class AsyncRequestTests
	{
		private const string BaseUrl = "http://localhost:8895/";

		[Fact]
		public void Async_POST_With_Body()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddParameter("name", "async_test");

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.Contains("name=async_test", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_PUT_With_Body()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.PUT);
				request.AddParameter("name", "async_test");

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.Contains("name=async_test", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_PATCH_With_Body()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.PATCH);
				request.AddParameter("name", "async_test");

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.Contains("name=async_test", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_DELETE()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.EchoMethod))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.DELETE);

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.Equal("DELETE", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_HEAD()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.EchoValue("not visible")))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.HEAD);

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.True(string.IsNullOrEmpty(response.Content));
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_OPTIONS()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.EchoMethod))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.OPTIONS);

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.Equal("OPTIONS", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}

		[Fact]
		public void Async_Timeout_Handling()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.TimeoutHandler(5000)))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);
				request.Timeout = 500;

				client.ExecuteAsync(request, (response, handle) =>
				{
					Assert.NotEqual(ResponseStatus.Completed, response.ResponseStatus);
					resetEvent.Set();
				});
				resetEvent.WaitOne(10000);
			}
		}

		[Fact]
		public void Async_ExecuteAsync_Simplified_Callback()
		{
			var resetEvent = new ManualResetEvent(false);
			using (SimpleServer.Create(BaseUrl, Handlers.EchoValue("simple callback")))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.GET);

				client.ExecuteAsync(request, response =>
				{
					Assert.Equal("simple callback", response.Content);
					resetEvent.Set();
				});
				resetEvent.WaitOne();
			}
		}
	}
}
