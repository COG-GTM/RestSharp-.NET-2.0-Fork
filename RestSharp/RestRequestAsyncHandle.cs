using System;
using System.Net;
using System.Threading;

namespace RestSharp
{
	/// <summary>
	/// Handle to an in-flight asynchronous request. <see cref="Abort"/> cancels the request via a <see cref="CancellationTokenSource"/>.
	/// </summary>
	public class RestRequestAsyncHandle
	{
		[Obsolete("Requests are sent with HttpClient; this is never populated by RestSharp. Use Abort() or CancellationToken.")]
		public HttpWebRequest WebRequest;

		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public RestRequestAsyncHandle()
		{
		}

		[Obsolete("Requests are sent with HttpClient; use the parameterless constructor.")]
		public RestRequestAsyncHandle(HttpWebRequest webRequest)
		{
			WebRequest = webRequest;
		}

		/// <summary>
		/// Token that is cancelled when <see cref="Abort"/> is called.
		/// </summary>
		public CancellationToken CancellationToken => _cancellationTokenSource.Token;

		/// <summary>
		/// True once <see cref="Abort"/> has been called.
		/// </summary>
		public bool IsAborted => _cancellationTokenSource.IsCancellationRequested;

		public void Abort()
		{
			_cancellationTokenSource.Cancel();
#pragma warning disable 618
			WebRequest?.Abort();
#pragma warning restore 618
		}

		internal void Link(RestRequestAsyncHandle inner)
		{
			if (inner == null || ReferenceEquals(inner, this))
				return;

			CancellationToken.Register(inner.Abort);
		}
	}
}
