using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp.IntegrationTests.Helpers
{
	public class SimpleServer : IDisposable
	{
		readonly HttpListener _listener;
		readonly Action<HttpListenerContext> _handler;
		readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
		Task _processor;

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
			if (_listener.IsListening)
			{
				return;
			}

			_listener.Start();
			_processor = Task.Run(async () =>
			{
				while (!_cancellation.IsCancellationRequested)
				{
					HttpListenerContext context;

					try
					{
						context = await _listener.GetContextAsync().ConfigureAwait(false);
					}
					catch (Exception)
					{
						return;
					}

					_handler(context);
					context.Response.Close();
				}
			});
		}

		public void Dispose()
		{
			_cancellation.Cancel();
			_listener.Stop();
			_listener.Close();
			_cancellation.Dispose();
		}
	}
}
