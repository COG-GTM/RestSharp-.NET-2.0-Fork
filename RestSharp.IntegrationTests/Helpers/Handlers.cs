using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace RestSharp.IntegrationTests.Helpers
{
	public static class Handlers
	{
		/// <summary>
		/// Echoes the request input back to the output.
		/// </summary>
		public static void Echo(HttpListenerContext context)
		{
			context.Request.InputStream.CopyTo(context.Response.OutputStream);
		}

		/// <summary>
		/// Echoes the given value back to the output.
		/// </summary>
		public static Action<HttpListenerContext> EchoValue(string value)
		{
			return ctx => ctx.Response.OutputStream.WriteStringUtf8(value);
		}

		/// <summary>
		/// Response to a request like this:  http://localhost:8080/assets/koala.jpg
		/// by streaming the file located at "assets\koala.jpg" back to the client.
		/// </summary>
		public static void FileHandler(HttpListenerContext context)
		{
			var pathToFile = Path.Combine(context.Request.Url.Segments.Select(s => s.Replace("/", "")).ToArray());

			using(var reader = new StreamReader(pathToFile))
				reader.BaseStream.CopyTo(context.Response.OutputStream);
		}

		/// <summary>
		/// Echoes request headers back as response body.
		/// </summary>
		public static void EchoHeaders(HttpListenerContext context)
		{
			var sb = new StringBuilder();
			foreach (string key in context.Request.Headers.AllKeys)
			{
				sb.AppendFormat("{0}: {1}\n", key, context.Request.Headers[key]);
			}
			context.Response.OutputStream.WriteStringUtf8(sb.ToString());
		}

		/// <summary>
		/// Echoes request cookies back as response body.
		/// </summary>
		public static void EchoCookies(HttpListenerContext context)
		{
			var sb = new StringBuilder();
			foreach (Cookie cookie in context.Request.Cookies)
			{
				sb.AppendFormat("{0}={1}\n", cookie.Name, cookie.Value);
			}
			context.Response.OutputStream.WriteStringUtf8(sb.ToString());
		}

		/// <summary>
		/// Echoes the HTTP method used.
		/// </summary>
		public static void EchoMethod(HttpListenerContext context)
		{
			context.Response.OutputStream.WriteStringUtf8(context.Request.HttpMethod);
		}

		/// <summary>
		/// Returns a handler that responds with a 302 redirect N times then 200.
		/// </summary>
		public static Action<HttpListenerContext> RedirectHandler(int times, string baseUrl)
		{
			var count = 0;
			return ctx =>
			{
				count++;
				if (count <= times)
				{
					ctx.Response.StatusCode = 302;
					ctx.Response.RedirectLocation = baseUrl;
				}
				else
				{
					ctx.Response.StatusCode = 200;
					ctx.Response.OutputStream.WriteStringUtf8("Done");
				}
			};
		}

		/// <summary>
		/// Returns a handler that delays the response by delayMs milliseconds.
		/// </summary>
		public static Action<HttpListenerContext> TimeoutHandler(int delayMs)
		{
			return ctx =>
			{
				Thread.Sleep(delayMs);
				ctx.Response.OutputStream.WriteStringUtf8("Delayed");
			};
		}

		/// <summary>
		/// Returns a handler that sets a cookie in the response.
		/// </summary>
		public static Action<HttpListenerContext> SetCookieHandler(string name, string value)
		{
			return ctx =>
			{
				var cookie = new Cookie(name, value, "/");
				ctx.Response.SetCookie(cookie);
				ctx.Response.OutputStream.WriteStringUtf8("OK");
			};
		}

		/// <summary>
		/// Returns a handler that returns a specific status code.
		/// </summary>
		public static Action<HttpListenerContext> StatusCodeHandler(int code)
		{
			return ctx =>
			{
				ctx.Response.StatusCode = code;
				ctx.Response.OutputStream.WriteStringUtf8("Status " + code);
			};
		}

		/// <summary>
		/// T should be a class that implements methods whose names match the urls being called, and take one parameter, an HttpListenerContext.
		/// e.g.
		/// urls exercised:  "http://localhost:8080/error"  and "http://localhost:8080/get_list"
		/// 
		/// class MyHandler
		/// {
		///   void error(HttpListenerContext ctx)
		///   {
		///     // do something interesting here
		///   }
		///
		///   void get_list(HttpListenerContext ctx)
		///   {
		///     // do something interesting here
		///   }
		/// }
		/// </summary>
		public static Action<HttpListenerContext> Generic<T>() where T : new()
		{
			return ctx =>
			{
				var methodName = ctx.Request.Url.Segments.Last();
				var method = typeof(T).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

				if(method.IsStatic)
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
}