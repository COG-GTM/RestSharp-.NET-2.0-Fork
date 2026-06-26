using System;
using System.Net;
using System.Threading;

namespace RestSharp.IntegrationTests.Helpers
{
	public class SimpleServer : IDisposable
	{
		readonly HttpListener _listener;
		readonly Action<HttpListenerContext> _handler;
		Thread _processor;

		public static SimpleServer Create(string url, Action<HttpListenerContext> handler)
		{
			var server = new SimpleServer(new HttpListener { Prefixes = { url } }, handler);
			server.Start();
			return server;
		}

		SimpleServer(HttpListener listener, Action<HttpListenerContext> handler)
		{
			_listener = listener;
			_handler = handler;
		}

		public void Start()
		{
			if(!_listener.IsListening)
			{
				_listener.Start();

				_processor = new Thread(() =>
				{
					try
					{
						var context = _listener.GetContext();
						_handler(context);
						context.Response.Close();
					}
					catch (HttpListenerException)
					{
						// Listener was stopped while waiting for a request.
					}
					catch (ObjectDisposedException)
					{
						// Listener was disposed while waiting for a request.
					}
				}) { Name = "WebServer", IsBackground = true };
				_processor.Start();
			}
		}

		public void Dispose()
		{
			_listener.Stop();
			_listener.Close();

			if (_processor != null)
				_processor.Join(TimeSpan.FromSeconds(5));
		}
	}
}