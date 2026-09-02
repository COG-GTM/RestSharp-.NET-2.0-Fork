using System;

namespace RestSharp
{
	public class RestRequestAsyncHandle
	{
		public HttpRequestHandle WebRequest;
		
		public RestRequestAsyncHandle()
		{
		}
		
		public RestRequestAsyncHandle(HttpRequestHandle webRequest)
		{
			WebRequest = webRequest;
		}
		
		public void Abort()
		{
			if (WebRequest != null)
				WebRequest.Abort();
		}
	}
}
