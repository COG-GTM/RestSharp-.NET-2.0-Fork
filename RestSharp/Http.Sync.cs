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

namespace RestSharp
{
	/// <summary>
	/// HttpClient wrapper (sync methods)
	/// </summary>
	public partial class Http
	{
		/// <summary>
		/// Execute a POST request
		/// </summary>
		public HttpResponse Post()
		{
			return SendRequest(HttpMethod.Post, true);
		}

		/// <summary>
		/// Execute a PUT request
		/// </summary>
		public HttpResponse Put()
		{
			return SendRequest(HttpMethod.Put, true);
		}

		/// <summary>
		/// Execute a GET request
		/// </summary>
		public HttpResponse Get()
		{
			return SendRequest(HttpMethod.Get, false);
		}

		/// <summary>
		/// Execute a HEAD request
		/// </summary>
		public HttpResponse Head()
		{
			return SendRequest(HttpMethod.Head, false);
		}

		/// <summary>
		/// Execute an OPTIONS request
		/// </summary>
		public HttpResponse Options()
		{
			return SendRequest(new HttpMethod("OPTIONS"), false);
		}

		/// <summary>
		/// Execute a DELETE request
		/// </summary>
		public HttpResponse Delete()
		{
			return SendRequest(HttpMethod.Delete, false);
		}

		/// <summary>
		/// Execute a PATCH request
		/// </summary>
		public HttpResponse Patch()
		{
			return SendRequest(new HttpMethod("PATCH"), true);
		}

		private HttpResponse SendRequest(HttpMethod method, bool sendBody)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };

			try
			{
				var handler = CreateHandler();
				using (handler)
				using (var client = CreateClient(handler))
				using (var message = BuildRequestMessage(method, sendBody))
				using (var webResponse = client.Send(message, HttpCompletionOption.ResponseContentRead))
				{
					ExtractResponseData(response, webResponse, handler);
				}
			}
			catch (Exception ex)
			{
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
				response.ResponseStatus = ResponseStatus.Error;
			}

			return response;
		}
	}
}
