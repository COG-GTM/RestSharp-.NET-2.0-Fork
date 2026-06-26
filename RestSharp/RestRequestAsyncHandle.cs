using System.Threading;

namespace RestSharp
{
	public class RestRequestAsyncHandle
	{
		public CancellationTokenSource CancellationTokenSource;

		public RestRequestAsyncHandle()
		{
		}

		public RestRequestAsyncHandle(CancellationTokenSource cancellationTokenSource)
		{
			CancellationTokenSource = cancellationTokenSource;
		}

		public void Abort()
		{
			if (CancellationTokenSource != null)
				CancellationTokenSource.Cancel();
		}
	}
}
