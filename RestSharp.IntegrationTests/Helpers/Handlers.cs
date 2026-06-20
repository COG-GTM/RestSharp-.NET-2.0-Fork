using System.Net;
using System.Reflection;

namespace RestSharp.IntegrationTests.Helpers;

public static class Handlers
{
    public static void Echo(HttpListenerContext context)
    {
        context.Request.InputStream.CopyTo(context.Response.OutputStream);
    }

    public static Action<HttpListenerContext> EchoValue(string value)
    {
        return ctx => ctx.Response.OutputStream.WriteStringUtf8(value);
    }

    public static void FileHandler(HttpListenerContext context)
    {
        var pathToFile = Path.Combine(
            context.Request.Url!.Segments.Select(s => s.Replace("/", "")).ToArray());

        using var reader = new StreamReader(pathToFile);
        reader.BaseStream.CopyTo(context.Response.OutputStream);
    }

    public static Action<HttpListenerContext> Generic<T>() where T : new()
    {
        return ctx =>
        {
            var methodName = ctx.Request.Url!.Segments.Last();
            var method = typeof(T).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if (method!.IsStatic)
            {
                method.Invoke(null, new object[] { ctx });
            }
            else
            {
                method.Invoke(new T(), new object[] { ctx });
            }
        };
    }
}
