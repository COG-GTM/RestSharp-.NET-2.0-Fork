#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp
{
	/// <summary>
	/// HttpClient wrapper (async methods)
	/// </summary>
	public partial class Http
	{
		public RestRequestAsyncHandle DeleteAsync(Action<HttpResponse> action)
		{
			return StartAsync("DELETE", action);
		}

		public RestRequestAsyncHandle GetAsync(Action<HttpResponse> action)
		{
			return StartAsync("GET", action);
		}

		public RestRequestAsyncHandle HeadAsync(Action<HttpResponse> action)
		{
			return StartAsync("HEAD", action);
		}

		public RestRequestAsyncHandle OptionsAsync(Action<HttpResponse> action)
		{
			return StartAsync("OPTIONS", action);
		}

		public RestRequestAsyncHandle PostAsync(Action<HttpResponse> action)
		{
			return StartAsync("POST", action);
		}

		public RestRequestAsyncHandle PutAsync(Action<HttpResponse> action)
		{
			return StartAsync("PUT", action);
		}

		public RestRequestAsyncHandle PatchAsync(Action<HttpResponse> action)
		{
			return StartAsync("PATCH", action);
		}

		private RestRequestAsyncHandle StartAsync(string method, Action<HttpResponse> callback)
		{
			var handle = new RestRequestAsyncHandle();
			_ = ExecuteWithCallbackAsync(method, handle.CancellationToken, callback);
			return handle;
		}

		private async Task ExecuteWithCallbackAsync(string method, CancellationToken cancellationToken, Action<HttpResponse> callback)
		{
			var response = await SendRequestAsync(method, cancellationToken).ConfigureAwait(false);
			callback(response);
		}
	}
}
