using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class SmokeTest
	{
		[Fact]
		public void Smoke_RestClient_Can_Be_Created()
		{
			var client = new RestClient("http://example.com");
			Assert.Equal("http://example.com", client.BaseUrl);
		}
	}
}
