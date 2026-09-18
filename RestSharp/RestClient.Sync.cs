using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp
{
	public partial class RestClient
	{
		/// <summary>
		/// Proxy to use for requests made by this client instance.
		/// </summary>
		public IWebProxy Proxy { get; set; }

		/// <summary>
		/// Executes the specified request and downloads the response data
		/// </summary>
		/// <param name="request">Request to execute</param>
		/// <returns>Response data</returns>
		public byte[] DownloadData(IRestRequest request)
		{
			return RunSync(() => DownloadDataAsync(request));
		}

		/// <summary>
		/// Executes the request and returns a response, authenticating if needed
		/// </summary>
		/// <param name="request">Request to be executed</param>
		/// <returns>RestResponse</returns>
		public virtual RestResponse Execute(IRestRequest request)
		{
			return RunSync(() => ExecuteAsync(request));
		}

		/// <summary>
		/// Executes the specified request and deserializes the response content using the appropriate content handler
		/// </summary>
		/// <typeparam name="T">Target deserialization type</typeparam>
		/// <param name="request">Request to execute</param>
		/// <returns>RestResponse[[T]] with deserialized data in Data property</returns>
		public virtual RestResponse<T> Execute<T>(IRestRequest request) where T : new()
		{
			return RunSync(() => ExecuteAsync<T>(request));
		}

		// Runs the asynchronous pipeline on the thread pool so that a captured
		// SynchronizationContext (WinForms, WPF, ASP.NET classic) cannot deadlock the caller.
		private static TResult RunSync<TResult>(System.Func<Task<TResult>> operation)
		{
			return Task.Run(operation).GetAwaiter().GetResult();
		}

		private void ConfigureProxy(IHttp http)
		{
			if (Proxy != null)
			{
				http.Proxy = Proxy;
			}
		}
	}
}
