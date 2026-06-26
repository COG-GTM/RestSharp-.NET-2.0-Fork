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
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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

		private const string FormBoundary = "-----------------------------28947758029299";

		private static string GetMultipartFormContentType()
		{
			return string.Format("multipart/form-data; boundary={0}", FormBoundary);
		}

		private static string GetMultipartFileHeader(HttpFile file)
		{
			return string.Format("--{0}{4}Content-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"{4}Content-Type: {3}{4}{4}",
				FormBoundary, file.Name, file.FileName, file.ContentType ?? "application/octet-stream", _lineBreak);
		}

		private static string GetMultipartFormData(HttpParameter param)
		{
			return string.Format("--{0}{3}Content-Disposition: form-data; name=\"{1}\"{3}{3}{2}{3}",
				FormBoundary, param.Name, param.Value, _lineBreak);
		}

		private static string GetMultipartFooter()
		{
			return string.Format("--{0}--{1}", FormBoundary, _lineBreak);
		}

		private static void WriteStringTo(Stream stream, string toWrite)
		{
			var bytes = _defaultEncoding.GetBytes(toWrite);
			stream.Write(bytes, 0, bytes.Length);
		}

		private void WriteMultipartFormData(Stream requestStream)
		{
			foreach (var param in Parameters)
			{
				WriteStringTo(requestStream, GetMultipartFormData(param));
			}

			foreach (var file in Files)
			{
				// Add just the first part of this param, since we will write the file data directly to the Stream
				WriteStringTo(requestStream, GetMultipartFileHeader(file));

				// Write the file data directly to the Stream, rather than serializing it to a string.
				file.Writer(requestStream);
				WriteStringTo(requestStream, _lineBreak);
			}

			WriteStringTo(requestStream, GetMultipartFooter());
		}

		private string EncodeParameters()
		{
			var querystring = new StringBuilder();
			foreach (var p in Parameters)
			{
				if (querystring.Length > 1)
					querystring.Append("&");
				querystring.AppendFormat("{0}={1}", Uri.EscapeDataString(p.Name), Uri.EscapeDataString(p.Value));
			}

			return querystring.ToString();
		}

		/// <summary>
		/// Builds the <see cref="HttpClientHandler"/> applying the configured transport
		/// security settings. TLS 1.2 and TLS 1.3 are the only enabled protocols.
		/// </summary>
		private HttpClientHandler CreateHandler()
		{
			var handler = new HttpClientHandler
			{
				AllowAutoRedirect = FollowRedirects,
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
				SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
				UseCookies = true,
				CookieContainer = CookieContainer ?? new CookieContainer()
			};

			if (FollowRedirects && MaxRedirects.HasValue)
			{
				handler.MaxAutomaticRedirections = MaxRedirects.Value;
			}

			if (Credentials != null)
			{
				handler.Credentials = Credentials;
			}
			else
			{
				handler.UseDefaultCredentials = false;
			}

			if (Proxy != null)
			{
				handler.Proxy = Proxy;
				handler.UseProxy = true;
			}

			if (ClientCertificates != null)
			{
				handler.ClientCertificates.AddRange(ClientCertificates);
			}

			AddCookies(handler.CookieContainer);

			return handler;
		}

		private HttpClient CreateClient(HttpClientHandler handler)
		{
			var client = new HttpClient(handler, disposeHandler: false);

			if (Timeout != 0)
			{
				client.Timeout = TimeSpan.FromMilliseconds(Timeout);
			}

			return client;
		}

		private void AddCookies(CookieContainer container)
		{
			if (!HasCookies || Url == null)
				return;

			var cookieUri = new Uri(string.Format("{0}://{1}", Url.Scheme, Url.Host));
			foreach (var httpCookie in Cookies)
			{
				container.Add(cookieUri, new Cookie(httpCookie.Name, httpCookie.Value));
			}
		}

		private HttpContent BuildContent()
		{
			if (HasFiles)
			{
				byte[] bytes;
				using (var ms = new MemoryStream())
				{
					WriteMultipartFormData(ms);
					bytes = ms.ToArray();
				}

				var content = new ByteArrayContent(bytes);
				content.Headers.TryAddWithoutValidation("Content-Type", GetMultipartFormContentType());
				return content;
			}

			if (HasParameters)
			{
				var content = new StringContent(EncodeParameters(), _defaultEncoding);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
				return content;
			}

			if (HasBody)
			{
				var content = new StringContent(RequestBody, _defaultEncoding);
				if (!string.IsNullOrEmpty(RequestContentType))
				{
					if (!MediaTypeHeaderValue.TryParse(RequestContentType, out var mediaType))
					{
						content.Headers.Remove("Content-Type");
						content.Headers.TryAddWithoutValidation("Content-Type", RequestContentType);
					}
					else
					{
						content.Headers.ContentType = mediaType;
					}
				}

				return content;
			}

			return null;
		}

		private HttpRequestMessage BuildRequestMessage(HttpMethod method, bool sendBody)
		{
			var message = new HttpRequestMessage(method, Url);
			HttpContent content = sendBody ? BuildContent() : null;

			ApplyHeaders(message, content);

			message.Content = content;
			return message;
		}

		private void ApplyHeaders(HttpRequestMessage message, HttpContent content)
		{
			if (!string.IsNullOrEmpty(UserAgent))
			{
				message.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
			}

			foreach (var header in Headers)
			{
				// Content-Length is computed by HttpClient; setting it manually throws.
				if (string.Equals(header.Name, "Content-Length", StringComparison.OrdinalIgnoreCase))
					continue;

				if (string.Equals(header.Name, "Content-Type", StringComparison.OrdinalIgnoreCase))
				{
					if (content != null)
					{
						content.Headers.Remove("Content-Type");
						content.Headers.TryAddWithoutValidation("Content-Type", header.Value);
					}
					continue;
				}

				if (!message.Headers.TryAddWithoutValidation(header.Name, header.Value) && content != null)
				{
					content.Headers.TryAddWithoutValidation(header.Name, header.Value);
				}
			}
		}

		private void ExtractResponseData(HttpResponse response, HttpResponseMessage webResponse, HttpClientHandler handler)
		{
			response.ContentType = webResponse.Content.Headers.ContentType != null
				? webResponse.Content.Headers.ContentType.ToString()
				: null;
			response.ContentLength = webResponse.Content.Headers.ContentLength ?? 0;

			var contentEncoding = webResponse.Content.Headers.ContentEncoding;
			response.ContentEncoding = contentEncoding != null && contentEncoding.Any()
				? string.Join(",", contentEncoding)
				: null;

			response.RawBytes = webResponse.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
			response.StatusCode = webResponse.StatusCode;
			response.StatusDescription = webResponse.ReasonPhrase;
			response.ResponseUri = webResponse.RequestMessage != null ? webResponse.RequestMessage.RequestUri : Url;
			response.Server = webResponse.Headers.Server != null ? webResponse.Headers.Server.ToString() : null;
			response.ResponseStatus = ResponseStatus.Completed;

			var cookieUri = response.ResponseUri ?? Url;
			if (cookieUri != null)
			{
				foreach (Cookie cookie in handler.CookieContainer.GetCookies(cookieUri))
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

			foreach (var header in webResponse.Headers)
			{
				foreach (var value in header.Value)
				{
					response.Headers.Add(new HttpHeader { Name = header.Key, Value = value });
				}
			}

			foreach (var header in webResponse.Content.Headers)
			{
				foreach (var value in header.Value)
				{
					response.Headers.Add(new HttpHeader { Name = header.Key, Value = value });
				}
			}
		}
	}
}
