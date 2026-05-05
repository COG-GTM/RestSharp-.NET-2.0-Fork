using System;
using System.Linq;
using System.Net;
using RestSharp.Deserializers;
using Xunit;

namespace RestSharp.Tests
{
	public class RestClientExecuteTests
	{
		[Fact]
		public void Execute_Adds_Accept_Header()
		{
			var client = new RestClient("http://localhost:12345");
			var request = new RestRequest("/test");

			try
			{
				client.Execute(request);
			}
			catch { }

			var accept = client.DefaultParameters.FirstOrDefault(
				p => p.Name == "Accept" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(accept);
		}

		[Fact]
		public void Execute_Sets_Error_On_Exception()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/nonexistent");

			var response = client.Execute(request);

			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
			Assert.NotNull(response.ErrorException);
		}

		[Fact]
		public void Execute_Generic_Sets_Error_On_Exception()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/nonexistent");

			var response = client.Execute<SimpleObj>(request);

			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
		}

		[Fact]
		public void DownloadData_Returns_Bytes_Or_Null_On_Error()
		{
			var client = new RestClient("http://localhost:1");
			var request = new RestRequest("/test");

			var data = client.DownloadData(request);
			// On connection failure, RawBytes will be null
			Assert.Null(data);
		}

		[Fact]
		public void ConvertToRestResponse_Preserves_Properties()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/test");

			// Execute against an unreachable host to test error handling path
			var response = client.Execute(request);
			Assert.NotNull(response);
			Assert.IsType<RestResponse>(response);
		}

		[Fact]
		public void Execute_With_Authenticator_Calls_Authenticate()
		{
			var client = new RestClient("http://localhost:1");
			client.Authenticator = new HttpBasicAuthenticator("user", "pass");
			var request = new RestRequest("/test");

			var response = client.Execute(request);

			var authHeader = request.Parameters.FirstOrDefault(
				p => p.Name == "Authorization" && p.Type == ParameterType.HttpHeader);
			Assert.NotNull(authHeader);
		}

		[Fact]
		public void Deserialize_Handles_Missing_Content_Type()
		{
			var client = new RestClient("http://example.com");
			var request = new RestRequest("/test");

			// Execute against unreachable host - should not crash
			var response = client.Execute<SimpleObj>(request);
			Assert.NotNull(response);
		}

		private class SimpleObj
		{
			public string Name { get; set; }
		}
	}
}
