using System.Net;
using RestSharp.Tests.Fakes;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class ProxyTests
	{
		[Fact]
		public void Setting_Proxy_On_Client_Passes_Through_To_Http()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;
			client.Proxy = new WebProxy("http://proxy.example.com:8080");

			var request = new RestRequest("resource", Method.GET);
			client.Execute(request);

			Assert.NotNull(fakeHttp.Proxy);
			Assert.Equal("http://proxy.example.com:8080/", ((WebProxy)fakeHttp.Proxy).Address.ToString());
		}

		[Fact]
		public void Proxy_Null_By_Default()
		{
			var fakeHttp = new FakeHttp();
			var factory = new FakeHttpFactory(fakeHttp);
			var client = new RestClient("http://example.com");
			client.HttpFactory = factory;

			var request = new RestRequest("resource", Method.GET);
			client.Execute(request);

			Assert.Null(fakeHttp.Proxy);
		}
	}
}
