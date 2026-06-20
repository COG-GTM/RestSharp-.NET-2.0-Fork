using System.Net;

namespace RestSharp.IntegrationTests.Helpers;

public class SimpleServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly Action<HttpListenerContext> _handler;
    private Thread? _processor;

    public static SimpleServer Create(string url, Action<HttpListenerContext> handler)
    {
        var server = new SimpleServer(new HttpListener { Prefixes = { url } }, handler);
        server.Start();
        return server;
    }

    private SimpleServer(HttpListener listener, Action<HttpListenerContext> handler)
    {
        _listener = listener;
        _handler = handler;
    }

    public void Start()
    {
        if (!_listener.IsListening)
        {
            _listener.Start();

            _processor = new Thread(() =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        var context = _listener.GetContext();
                        _handler(context);
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                }
            })
            { Name = "WebServer", IsBackground = true };
            _processor.Start();
        }
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }
}
