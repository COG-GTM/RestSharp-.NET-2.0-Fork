using Xunit;

namespace RestSharp.IntegrationTests
{
	public class ErrorHandlingTests
	{
		[Fact]
		public void Connection_Refused_Returns_Error()
		{
			var client = new RestClient("http://localhost:19999/");
			var request = new RestRequest("", Method.GET);

			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
			Assert.NotNull(response.ErrorException);
		}

		[Fact]
		public void DNS_Resolution_Failure_Returns_Error()
		{
			var client = new RestClient("http://nonexistent.invalid.domain.test/");
			var request = new RestRequest("", Method.GET);

			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
			Assert.NotNull(response.ErrorMessage);
		}

		[Fact]
		public void Invalid_Url_Returns_Error()
		{
			var client = new RestClient("http://localhost:19998/");
			var request = new RestRequest("nonexistent", Method.GET);

			var response = client.Execute(request);
			Assert.Equal(ResponseStatus.Error, response.ResponseStatus);
		}
	}
}
