namespace RestSharp.Tests.Fakes
{
	public class FakeHttpFactory : IHttpFactory
	{
		private readonly FakeHttp _http;

		public FakeHttpFactory(FakeHttp http)
		{
			_http = http;
		}

		public IHttp Create()
		{
			return _http;
		}
	}
}
