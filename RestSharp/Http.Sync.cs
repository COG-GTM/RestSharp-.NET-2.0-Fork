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

using System.Threading;

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
			return PostPutInternal("POST");
		}

		/// <summary>
		/// Execute a PUT request
		/// </summary>
		public HttpResponse Put()
		{
			return PostPutInternal("PUT");
		}

		/// <summary>
		/// Execute a GET request
		/// </summary>
		public HttpResponse Get()
		{
			return GetStyleMethodInternal("GET");
		}

		/// <summary>
		/// Execute a HEAD request
		/// </summary>
		public HttpResponse Head()
		{
			return GetStyleMethodInternal("HEAD");
		}

		/// <summary>
		/// Execute an OPTIONS request
		/// </summary>
		public HttpResponse Options()
		{
			return GetStyleMethodInternal("OPTIONS");
		}

		/// <summary>
		/// Execute a DELETE request
		/// </summary>
		public HttpResponse Delete()
		{
			return GetStyleMethodInternal("DELETE");
		}

		/// <summary>
		/// Execute a PATCH request
		/// </summary>
		public HttpResponse Patch()
		{
			return PostPutInternal("PATCH");
		}

		private HttpResponse GetStyleMethodInternal(string method)
		{
			return Send(method);
		}

		private HttpResponse PostPutInternal(string method)
		{
			return Send(method);
		}

		// netstandard2.0 has no synchronous HttpClient.Send; every await in the transport uses
		// ConfigureAwait(false), so blocking here cannot deadlock on a SynchronizationContext.
		private HttpResponse Send(string method)
		{
			return SendRequestAsync(method, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
