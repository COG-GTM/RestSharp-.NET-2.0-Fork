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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp
{
	public partial class RestClient
	{
		/// <summary>
		/// Executes the request asynchronously, authenticating if needed
		/// </summary>
		/// <param name="request">Request to be executed</param>
		/// <param name="cancellationToken">Token used to cancel the request</param>
		public virtual async Task<RestResponse> ExecuteAsync(IRestRequest request, CancellationToken cancellationToken = default(CancellationToken))
		{
			AuthenticateIfNeeded(this, request);

			// add Accept header based on registered deserializers
			var accepts = string.Join(", ", AcceptTypes.ToArray());
			AddDefaultParameter("Accept", accepts, ParameterType.HttpHeader);

			var response = new RestResponse();

			try
			{
				response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);
				response.Request = request;
				response.Request.IncreaseNumAttempts();
			}
			catch (Exception ex)
			{
				response.ResponseStatus = ResponseStatus.Error;
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
			}

			return response;
		}

		/// <summary>
		/// Executes the request asynchronously and deserializes the response content, authenticating if needed
		/// </summary>
		/// <typeparam name="T">Target deserialization type</typeparam>
		/// <param name="request">Request to be executed</param>
		/// <param name="cancellationToken">Token used to cancel the request</param>
		public virtual async Task<RestResponse<T>> ExecuteAsync<T>(IRestRequest request, CancellationToken cancellationToken = default(CancellationToken)) where T : new()
		{
			var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

			if (response.ResponseStatus == ResponseStatus.Aborted)
			{
				return new RestResponse<T>
				{
					Request = response.Request,
					ResponseStatus = response.ResponseStatus,
					ErrorMessage = response.ErrorMessage,
					ErrorException = response.ErrorException
				};
			}

			return Deserialize<T>(request, response);
		}

		/// <summary>
		/// Executes the specified request asynchronously and downloads the response data
		/// </summary>
		/// <param name="request">Request to execute</param>
		/// <param name="cancellationToken">Token used to cancel the request</param>
		public async Task<byte[]> DownloadDataAsync(IRestRequest request, CancellationToken cancellationToken = default(CancellationToken))
		{
			var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
			return response.RawBytes;
		}

		private async Task<RestResponse> GetResponseAsync(IRestRequest request, CancellationToken cancellationToken)
		{
			var http = HttpFactory.Create();

			ConfigureHttp(request, http);
			ConfigureProxy(http);

			HttpResponse httpResponse;

			switch (request.Method)
			{
				case Method.POST:
					httpResponse = await http.PostAsync(cancellationToken).ConfigureAwait(false);
					break;
				case Method.PUT:
					httpResponse = await http.PutAsync(cancellationToken).ConfigureAwait(false);
					break;
				case Method.DELETE:
					httpResponse = await http.DeleteAsync(cancellationToken).ConfigureAwait(false);
					break;
				case Method.HEAD:
					httpResponse = await http.HeadAsync(cancellationToken).ConfigureAwait(false);
					break;
				case Method.OPTIONS:
					httpResponse = await http.OptionsAsync(cancellationToken).ConfigureAwait(false);
					break;
				case Method.PATCH:
					httpResponse = await http.PatchAsync(cancellationToken).ConfigureAwait(false);
					break;
				default:
					httpResponse = await http.GetAsync(cancellationToken).ConfigureAwait(false);
					break;
			}

			return ConvertToRestResponse(httpResponse);
		}
	}
}
