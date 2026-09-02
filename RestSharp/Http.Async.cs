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
		public HttpRequestHandle DeleteAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("DELETE", action);
		}

		public HttpRequestHandle GetAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("GET", action);
		}

		public HttpRequestHandle HeadAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("HEAD", action);
		}

		public HttpRequestHandle OptionsAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("OPTIONS", action);
		}

		public HttpRequestHandle PostAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("POST", action);
		}

		public HttpRequestHandle PutAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("PUT", action);
		}

		public HttpRequestHandle PatchAsync(Action<HttpResponse> action)
		{
			return ExecuteWithCallback("PATCH", action);
		}

		private HttpRequestHandle ExecuteWithCallback(string method, Action<HttpResponse> callback)
		{
			var cts = new CancellationTokenSource();
			var handle = new HttpRequestHandle(cts);
			ExecuteInternalAsync(method, handle.Token).ContinueWith(t =>
			{
				callback(t.Result);
				cts.Dispose();
			}, TaskScheduler.Default);
			return handle;
		}
	}
}
