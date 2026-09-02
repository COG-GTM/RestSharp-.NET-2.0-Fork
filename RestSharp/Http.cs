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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Extensions;

namespace RestSharp
{
	/// <summary>
	/// HttpClient wrapper
	/// </summary>
	public partial class Http : IHttp, IHttpFactory
	{
		private const string _lineBreak = "\r\n";
		private static readonly Encoding _defaultEncoding = Encoding.UTF8;

		///<summary>
		/// Creates an IHttp
		///</summary>
		///<returns></returns>
		public IHttp Create()
		{
			return new Http();
		}

		/// <summary>
		/// True if this HTTP request has any HTTP parameters
		/// </summary>
		protected bool HasParameters
		{
			get
			{
				return Parameters.Any();
			}
		}

		/// <summary>
		/// True if this HTTP request has any HTTP cookies
		/// </summary>
		protected bool HasCookies
		{
			get
			{
				return Cookies.Any();
			}
		}

		/// <summary>
		/// True if a request body has been specified
		/// </summary>
		protected bool HasBody
		{
			get
			{
				return !string.IsNullOrEmpty(RequestBody);
			}
		}

		/// <summary>
		/// True if files have been set to be uploaded
		/// </summary>
		protected bool HasFiles
		{
			get
			{
				return Files.Any();
			}
		}

		/// <summary>
		/// UserAgent to be sent with request
		/// </summary>
		public string UserAgent { get; set; }
		/// <summary>
		/// Timeout in milliseconds to be used for the request
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
		public int ReadWriteTimeout { get; set; }
		public bool UseDefaultCredentials { get; set; }
		public bool PreAuthenticate { get; set; }
		public bool AlwaysMultipartFormData { get; set; }
		public byte[] RequestBodyBytes { get; set; }
		public Encoding Encoding { get; set; }
		public Action<Stream> ResponseWriter { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public Http()
		{
			Headers = new List<HttpHeader>();
			Files = new List<HttpFile>();
			Parameters = new List<HttpParameter>();
			Cookies = new List<HttpCookie>();
			Encoding = _defaultEncoding;
		}

		private const string FormBoundary = "-----------------------------28947758029299";
		private static string GetMultipartFormContentType()
		{
			return string.Format("multipart/form-data; boundary={0}", FormBoundary);
		}
		
		private static string GetMultipartFileHeader (HttpFile file)
		{
			return string.Format ("--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}",
				FormBoundary, file.Name, file.FileName, file.ContentType ?? "application/octet-stream", _lineBreak);
		}
		
		private static string GetMultipartFormData (HttpParameter param)
		{
			return string.Format ("--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}",
				FormBoundary, param.Name, param.Value, _lineBreak);
		}
		
		private static string GetMultipartFooter ()
		{
			return string.Format ("--{0}--{1}", FormBoundary, _lineBreak);
		}

		private string EncodeParameters()
		{
			var querystring = new StringBuilder();
			foreach (var p in Parameters)
			{
				if (querystring.Length > 1)
					querystring.Append("&");
				querystring.AppendFormat("{0}={1}", p.Name.UrlEncode(), p.Value.UrlEncode());
			}

			return querystring.ToString();
		}

		public Task<HttpResponse> ExecuteAsync(string httpMethod, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ExecuteInternalAsync(httpMethod, cancellationToken);
		}

		private async Task<HttpResponse> ExecuteInternalAsync(string method, CancellationToken externalToken)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };

			try
			{
				CookieContainer container;
				using (var handler = CreateHandler(out container))
				using (var client = new HttpClient(handler, disposeHandler: false) { Timeout = System.Threading.Timeout.InfiniteTimeSpan })
				using (var linked = CancellationTokenSource.CreateLinkedTokenSource(externalToken))
				{
					if (Timeout > 0)
						linked.CancelAfter(Timeout);

					using (var request = BuildRequestMessage(method))
					using (var httpResponse = await client.SendAsync(request, ResponseWriter != null ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, linked.Token).ConfigureAwait(false))
					{
						await ExtractResponseDataAsync(response, httpResponse, linked.Token).ConfigureAwait(false);
						response.ResponseStatus = ResponseStatus.Completed;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
				response.ResponseStatus = externalToken.IsCancellationRequested ? ResponseStatus.Aborted : ResponseStatus.TimedOut;
			}
			catch (Exception ex)
			{
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
				response.ResponseStatus = ResponseStatus.Error;
			}

			return response;
		}

		private HttpClientHandler CreateHandler(out CookieContainer container)
		{
			var handler = new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				AllowAutoRedirect = FollowRedirects,
				UseCookies = true,
				PreAuthenticate = PreAuthenticate
			};

			if (FollowRedirects && MaxRedirects.HasValue)
				handler.MaxAutomaticRedirections = MaxRedirects.Value;
			if (Proxy != null)
			{
				handler.Proxy = Proxy;
				handler.UseProxy = true;
			}
			if (Credentials != null)
				handler.Credentials = Credentials;
			handler.UseDefaultCredentials = UseDefaultCredentials;

			container = CookieContainer ?? new CookieContainer();
			foreach (var httpCookie in Cookies)
			{
				container.Add(new Cookie
				{
					Name = httpCookie.Name,
					Value = httpCookie.Value,
					Domain = Url.Host
				});
			}
			handler.CookieContainer = container;

			if (ClientCertificates != null)
				handler.ClientCertificates.AddRange(ClientCertificates);

			return handler;
		}

		private HttpRequestMessage BuildRequestMessage(string method)
		{
			var request = new HttpRequestMessage(new HttpMethod(method), Url);
			request.Headers.ExpectContinue = false;
			if (UserAgent.HasValue())
				request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

			if (method == "POST" || method == "PUT" || method == "PATCH")
			{
				HttpContent content;
				if (HasFiles || AlwaysMultipartFormData)
				{
					content = new RestMultipartContent(this);
				}
				else if (HasParameters)
				{
					RequestBody = EncodeParameters();
					content = new ByteArrayContent(_defaultEncoding.GetBytes(RequestBody));
					SetContentType(content, "application/x-www-form-urlencoded");
				}
				else if (RequestBodyBytes != null)
				{
					content = new ByteArrayContent(RequestBodyBytes);
					SetContentType(content, RequestContentType);
				}
				else if (HasBody)
				{
					content = new ByteArrayContent((Encoding ?? _defaultEncoding).GetBytes(RequestBody));
					SetContentType(content, RequestContentType);
				}
				else
				{
					content = new ByteArrayContent(new byte[0]);
				}

				request.Content = content;
			}

			foreach (var header in Headers)
				ApplyHeader(request, header.Name, header.Value);

			return request;
		}

		private static void SetContentType(HttpContent content, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				content.Headers.Remove("Content-Type");
				content.Headers.TryAddWithoutValidation("Content-Type", value);
			}
		}

		private static void ApplyHeader(HttpRequestMessage request, string name, string value)
		{
			if (string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
				return;

			if (string.Equals(name, "Range", StringComparison.OrdinalIgnoreCase))
			{
				var match = Regex.Match(value, "=(\\d+)-(\\d+)$");
				if (match.Success)
					request.Headers.Range = new RangeHeaderValue(Convert.ToInt64(match.Groups[1].Value), Convert.ToInt64(match.Groups[2].Value));
				return;
			}

			if (string.Equals(name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
			{
				if (string.Equals(value, "chunked", StringComparison.OrdinalIgnoreCase))
					request.Headers.TransferEncodingChunked = true;
				else
				{
					request.Headers.Remove(name);
					request.Headers.TryAddWithoutValidation(name, value);
				}
				return;
			}

			switch (name.ToLowerInvariant())
			{
				case "content-type":
				case "content-encoding":
				case "content-language":
				case "content-location":
				case "content-md5":
				case "content-range":
				case "content-disposition":
				case "expires":
				case "last-modified":
				case "allow":
					if (request.Content != null)
					{
						request.Content.Headers.Remove(name);
						request.Content.Headers.TryAddWithoutValidation(name, value);
					}
					return;
			}

			request.Headers.Remove(name);
			request.Headers.TryAddWithoutValidation(name, value);
		}

		private async Task ExtractResponseDataAsync(HttpResponse response, HttpResponseMessage msg, CancellationToken ct)
		{
			response.StatusCode = msg.StatusCode;
			response.StatusDescription = msg.ReasonPhrase;
			response.ResponseUri = msg.RequestMessage != null && msg.RequestMessage.RequestUri != null ? msg.RequestMessage.RequestUri : Url;
			response.Server = msg.Headers.Server != null ? msg.Headers.Server.ToString() : null;

			var content = msg.Content;
			if (content == null)
			{
				response.RawBytes = new byte[0];
			}
			else
			{
				response.ContentType = content.Headers.ContentType != null ? content.Headers.ContentType.ToString() : null;
				response.ContentEncoding = string.Join(",", content.Headers.ContentEncoding);
				if (ResponseWriter != null)
				{
					using (var stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
						ResponseWriter(stream);
				}
				else
				{
					response.RawBytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
				}

				response.ContentLength = content.Headers.ContentLength ?? (response.RawBytes != null ? response.RawBytes.LongLength : -1);
			}

			foreach (var header in msg.Headers)
				response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(", ", header.Value) });
			if (content != null)
			{
				foreach (var header in content.Headers)
					response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(", ", header.Value) });
			}

			IEnumerable<string> setCookies;
			if (msg.Headers.TryGetValues("Set-Cookie", out setCookies))
			{
				var jar = new CookieContainer();
				foreach (var setCookie in setCookies)
				{
					try
					{
						jar.SetCookies(response.ResponseUri, setCookie);
					}
					catch (CookieException)
					{
					}
				}

				foreach (Cookie cookie in jar.GetCookies(response.ResponseUri))
				{
					response.Cookies.Add(new HttpCookie {
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

		private sealed class RestMultipartContent : HttpContent
		{
			private readonly Http _http;

			public RestMultipartContent(Http http)
			{
				_http = http;
				Headers.TryAddWithoutValidation("Content-Type", GetMultipartFormContentType());
			}

			protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
			{
				foreach (var param in _http.Parameters)
					WriteStringTo(stream, GetMultipartFormData(param));

				foreach (var file in _http.Files)
				{
					WriteStringTo(stream, GetMultipartFileHeader(file));
					file.Writer(stream);
					WriteStringTo(stream, _lineBreak);
				}

				WriteStringTo(stream, GetMultipartFooter());
				return Task.CompletedTask;
			}

			protected override bool TryComputeLength(out long length)
			{
				length = 0;
				foreach (var file in _http.Files)
				{
					length += _defaultEncoding.GetByteCount(GetMultipartFileHeader(file));
					length += file.ContentLength;
					length += _defaultEncoding.GetByteCount(_lineBreak);
				}

				foreach (var param in _http.Parameters)
					length += _defaultEncoding.GetByteCount(GetMultipartFormData(param));

				length += _defaultEncoding.GetByteCount(GetMultipartFooter());
				return true;
			}

			private static void WriteStringTo(Stream stream, string value)
			{
				var bytes = _defaultEncoding.GetBytes(value);
				stream.Write(bytes, 0, bytes.Length);
			}
		}
	}
}
