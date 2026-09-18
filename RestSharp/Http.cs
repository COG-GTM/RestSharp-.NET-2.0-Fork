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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Extensions;

namespace RestSharp
{
	/// <summary>
	/// HttpClient wrapper
	/// </summary>
	public class Http : IHttp, IHttpFactory
	{
		private static readonly Encoding _defaultEncoding = Encoding.UTF8;

		private static readonly HashSet<string> _contentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length",
			"Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified"
		};

		///<summary>
		/// Creates an IHttp
		///</summary>
		public IHttp Create()
		{
			return new Http();
		}

		/// <summary>
		/// True if this HTTP request has any HTTP parameters
		/// </summary>
		protected bool HasParameters
		{
			get { return Parameters.Any(); }
		}

		/// <summary>
		/// True if this HTTP request has any HTTP cookies
		/// </summary>
		protected bool HasCookies
		{
			get { return Cookies.Any(); }
		}

		/// <summary>
		/// True if a request body has been specified
		/// </summary>
		protected bool HasBody
		{
			get { return !string.IsNullOrEmpty(RequestBody); }
		}

		/// <summary>
		/// True if files have been set to be uploaded
		/// </summary>
		protected bool HasFiles
		{
			get { return Files.Any(); }
		}

		/// <summary>
		/// UserAgent to be sent with request
		/// </summary>
		public string UserAgent { get; set; }
		/// <summary>
		/// Timeout in milliseconds to be used for the request. Zero means the HttpClient default.
		/// </summary>
		public int Timeout { get; set; }
		/// <summary>
		/// System.Net.ICredentials to be sent with request
		/// </summary>
		public ICredentials Credentials { get; set; }
		/// <summary>
		/// The System.Net.CookieContainer to be used for the request
		/// </summary>
		public CookieContainer CookieContainer { get; set; }
		/// <summary>
		/// Collection of files to be sent with request
		/// </summary>
		public IList<HttpFile> Files { get; private set; }
		/// <summary>
		/// Whether or not HTTP 3xx response redirects should be automatically followed
		/// </summary>
		public bool FollowRedirects { get; set; }
		/// <summary>
		/// X509CertificateCollection to be sent with request
		/// </summary>
		public X509CertificateCollection ClientCertificates { get; set; }
		/// <summary>
		/// Maximum number of automatic redirects to follow if FollowRedirects is true
		/// </summary>
		public int? MaxRedirects { get; set; }
		/// <summary>
		/// HTTP headers to be sent with request
		/// </summary>
		public IList<HttpHeader> Headers { get; private set; }
		/// <summary>
		/// HTTP parameters (QueryString or Form values) to be sent with request
		/// </summary>
		public IList<HttpParameter> Parameters { get; private set; }
		/// <summary>
		/// HTTP cookies to be sent with request
		/// </summary>
		public IList<HttpCookie> Cookies { get; private set; }
		/// <summary>
		/// Request body to be sent with request
		/// </summary>
		public string RequestBody { get; set; }
		/// <summary>
		/// Content type of the request body.
		/// </summary>
		public string RequestContentType { get; set; }
		/// <summary>
		/// URL to call for this request
		/// </summary>
		public Uri Url { get; set; }
		/// <summary>
		/// Proxy info to be sent with request
		/// </summary>
		public IWebProxy Proxy { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public Http()
		{
			Headers = new List<HttpHeader>();
			Files = new List<HttpFile>();
			Parameters = new List<HttpParameter>();
			Cookies = new List<HttpCookie>();
		}

		/// <summary>
		/// Execute a GET request
		/// </summary>
		public Task<HttpResponse> GetAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Get, cancellationToken);
		}

		/// <summary>
		/// Execute a HEAD request
		/// </summary>
		public Task<HttpResponse> HeadAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Head, cancellationToken);
		}

		/// <summary>
		/// Execute an OPTIONS request
		/// </summary>
		public Task<HttpResponse> OptionsAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Options, cancellationToken);
		}

		/// <summary>
		/// Execute a DELETE request
		/// </summary>
		public Task<HttpResponse> DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Delete, cancellationToken);
		}

		/// <summary>
		/// Execute a POST request
		/// </summary>
		public Task<HttpResponse> PostAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Post, cancellationToken);
		}

		/// <summary>
		/// Execute a PUT request
		/// </summary>
		public Task<HttpResponse> PutAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(HttpMethod.Put, cancellationToken);
		}

		/// <summary>
		/// Execute a PATCH request
		/// </summary>
		public Task<HttpResponse> PatchAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SendAsync(new HttpMethod("PATCH"), cancellationToken);
		}

		private async Task<HttpResponse> SendAsync(HttpMethod method, CancellationToken cancellationToken)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };
			var cookieContainer = CookieContainer ?? new CookieContainer();

			AppendCookies(cookieContainer);

			using (var handler = CreateHandler(cookieContainer))
			using (var client = new HttpClient(handler, disposeHandler: false))
			using (var message = new HttpRequestMessage(method, Url))
			{
				if (Timeout != 0)
				{
					client.Timeout = TimeSpan.FromMilliseconds(Timeout);
				}

				message.Content = BuildContent(method);
				AppendHeaders(message);

				try
				{
					using (var httpResponse = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
					{
						await ExtractResponseData(response, httpResponse, cookieContainer).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
				{
					// HttpClient surfaces its own timeout as a cancellation that the caller did not ask for
					response.ResponseStatus = ResponseStatus.TimedOut;
					response.ErrorMessage = ex.Message;
					response.ErrorException = ex;
				}
				catch (OperationCanceledException ex)
				{
					response.ResponseStatus = ResponseStatus.Aborted;
					response.ErrorMessage = ex.Message;
					response.ErrorException = ex;
				}
				catch (Exception ex)
				{
					response.ResponseStatus = ResponseStatus.Error;
					response.ErrorMessage = ex.Message;
					response.ErrorException = ex;
				}
			}

			return response;
		}

		private HttpClientHandler CreateHandler(CookieContainer cookieContainer)
		{
			var handler = new HttpClientHandler
			{
				AllowAutoRedirect = FollowRedirects,
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				CookieContainer = cookieContainer,
				UseCookies = true,
				UseDefaultCredentials = false
			};

			if (FollowRedirects && MaxRedirects.HasValue)
			{
				handler.MaxAutomaticRedirections = MaxRedirects.Value;
			}

			if (Credentials != null)
			{
				handler.Credentials = Credentials;
			}

			if (Proxy != null)
			{
				handler.Proxy = Proxy;
				handler.UseProxy = true;
			}

			if (ClientCertificates != null)
			{
				foreach (X509Certificate certificate in ClientCertificates)
				{
					handler.ClientCertificates.Add(certificate);
				}
			}

			return handler;
		}

		private HttpContent BuildContent(HttpMethod method)
		{
			if (HasFiles)
			{
				var content = new MultipartFormDataContent();

				foreach (var parameter in Parameters)
				{
					content.Add(new StringContent(parameter.Value ?? string.Empty, _defaultEncoding), parameter.Name);
				}

				foreach (var file in Files)
				{
					using (var stream = new MemoryStream())
					{
						file.Writer(stream);

						var fileContent = new ByteArrayContent(stream.ToArray());
						fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType ?? "application/octet-stream");
						content.Add(fileContent, file.Name, file.FileName);
					}
				}

				return content;
			}

			if (HasParameters && !IsGetStyle(method))
			{
				return new FormUrlEncodedContent(Parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value)));
			}

			if (HasBody)
			{
				var content = new StringContent(RequestBody, _defaultEncoding);

				if (RequestContentType.HasValue())
				{
					content.Headers.ContentType = MediaTypeHeaderValue.Parse(RequestContentType);
				}

				return content;
			}

			return null;
		}

		private static bool IsGetStyle(HttpMethod method)
		{
			return method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options || method == HttpMethod.Delete;
		}

		private void AppendHeaders(HttpRequestMessage message)
		{
			if (UserAgent.HasValue())
			{
				message.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
			}

			foreach (var header in Headers)
			{
				if (_contentHeaders.Contains(header.Name))
				{
					if (message.Content != null)
					{
						message.Content.Headers.Remove(header.Name);
						message.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
					}

					continue;
				}

				message.Headers.TryAddWithoutValidation(header.Name, header.Value);
			}
		}

		private void AppendCookies(CookieContainer cookieContainer)
		{
			foreach (var httpCookie in Cookies)
			{
				cookieContainer.Add(new Cookie
				{
					Name = httpCookie.Name,
					Value = httpCookie.Value,
					Domain = Url.Host
				});
			}
		}

		private static async Task ExtractResponseData(HttpResponse response, HttpResponseMessage httpResponse, CookieContainer cookieContainer)
		{
			response.RawBytes = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			response.StatusCode = httpResponse.StatusCode;
			response.StatusDescription = httpResponse.ReasonPhrase;
			response.ResponseUri = httpResponse.RequestMessage != null ? httpResponse.RequestMessage.RequestUri : null;
			response.ResponseStatus = ResponseStatus.Completed;

			var contentHeaders = httpResponse.Content.Headers;
			response.ContentType = contentHeaders.ContentType != null ? contentHeaders.ContentType.ToString() : null;
			response.ContentLength = contentHeaders.ContentLength ?? response.RawBytes.Length;
			response.ContentEncoding = string.Join(",", contentHeaders.ContentEncoding.ToArray());
			response.Server = httpResponse.Headers.Server != null ? httpResponse.Headers.Server.ToString() : null;

			foreach (var header in httpResponse.Headers.Concat(contentHeaders))
			{
				response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(",", header.Value.ToArray()) });
			}

			if (response.ResponseUri != null)
			{
				foreach (Cookie cookie in cookieContainer.GetCookies(response.ResponseUri))
				{
					response.Cookies.Add(new HttpCookie
					{
						Comment = cookie.Comment,
						CommentUri = cookie.CommentUri,
						Discard = cookie.Discard,
						Domain = cookie.Domain,
						Expired = cookie.Expired,
						Expires = cookie.Expires,
						HttpOnly = cookie.HttpOnly,
						Name = cookie.Name,
						Path = cookie.Path,
						Port = cookie.Port,
						Secure = cookie.Secure,
						TimeStamp = cookie.TimeStamp,
						Value = cookie.Value,
						Version = cookie.Version
					});
				}
			}
		}
	}
}
