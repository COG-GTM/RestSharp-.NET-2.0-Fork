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
using System.Net.Http;
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
			return SendRequestAsync(HttpMethod.Delete, false, action);
		}

		public RestRequestAsyncHandle GetAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(HttpMethod.Get, false, action);
		}

		public RestRequestAsyncHandle HeadAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(HttpMethod.Head, false, action);
		}

		public RestRequestAsyncHandle OptionsAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(new HttpMethod("OPTIONS"), false, action);
		}

		public RestRequestAsyncHandle PostAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(HttpMethod.Post, true, action);
		}

		public RestRequestAsyncHandle PutAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(HttpMethod.Put, true, action);
		}

		public RestRequestAsyncHandle PatchAsync(Action<HttpResponse> action)
		{
			return SendRequestAsync(new HttpMethod("PATCH"), true, action);
		}

		private RestRequestAsyncHandle SendRequestAsync(HttpMethod method, bool sendBody, Action<HttpResponse> callback)
		{
			var cts = new CancellationTokenSource();
			var handle = new RestRequestAsyncHandle(cts);

			RunRequestAsync(method, sendBody, callback, cts.Token);

			return handle;
		}

		private async void RunRequestAsync(HttpMethod method, bool sendBody, Action<HttpResponse> callback, CancellationToken token)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };

			try
			{
				var handler = CreateHandler();
				using (handler)
				using (var client = CreateClient(handler))
				using (var message = BuildRequestMessage(method, sendBody))
				using (var webResponse = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false))
				{
					ExtractResponseData(response, webResponse, handler);
				}
			}
			catch (OperationCanceledException)
			{
				response.ResponseStatus = ResponseStatus.Aborted;
			}
			catch (Exception ex)
			{
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
				response.ResponseStatus = ResponseStatus.Error;
			}

			callback(response);
		}
	}
}
