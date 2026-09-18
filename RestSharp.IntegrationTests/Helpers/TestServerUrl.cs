using System.Net;
using System.Net.Sockets;

namespace RestSharp.IntegrationTests.Helpers
{
	public static class TestServerUrl
	{
		/// <summary>
		/// Returns a loopback url on a port that is free at the time of the call, so tests
		/// running side by side never collide on a fixed HttpListener prefix.
		/// </summary>
		public static string Next()
		{
			var probe = new TcpListener(IPAddress.Loopback, 0);

			probe.Start();

			var port = ((IPEndPoint) probe.LocalEndpoint).Port;

			probe.Stop();

			return string.Format("http://localhost:{0}/", port);
		}
	}
}
