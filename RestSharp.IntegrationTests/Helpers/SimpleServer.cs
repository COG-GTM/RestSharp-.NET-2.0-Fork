using System;
using System.Net;
using System.Threading;

namespace RestSharp.IntegrationTests.Helpers
{
	public class SimpleServer : IDisposable
	{
		readonly HttpListener _listener;
		readonly Action<HttpListenerContext> _handler;
		readonly int _maxRequests;
		Thread _processor;

		public static SimpleServer Create(string url, Action<HttpListenerContext> handler, int maxRequests = 1)
		{
			var server = new SimpleServer(new HttpListener { Prefixes = { url } }, handler, maxRequests);
			server.Start();
			return server;
		}

		SimpleServer(HttpListener listener, Action<HttpListenerContext> handler, int maxRequests)
		{
			_listener = listener;
			_handler = handler;
			_maxRequests = maxRequests;
		}

		public void Start()
		{
			if(!_listener.IsListening)
			{
				_listener.Start();

				_processor = new Thread(() =>
				{
					for (int i = 0; i < _maxRequests; i++)
					{
						try
						{
							var context = _listener.GetContext();
							_handler(context);
							context.Response.Close();
						}
						catch
						{
							break;
						}
					}
				}) { Name = "WebServer" };
				_processor.Start();
			}
		}

		public void Dispose()
		{
			_processor.Abort();
			_listener.Stop();
			_listener.Close();
		}
	}
}