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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Extensions;

namespace RestSharp
{
	/// <summary>
	/// HttpClient transport shared by the sync and async entry points
	/// </summary>
	public partial class Http
	{
		private static readonly HashSet<string> _ignoredRequestHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Content-Length", // computed from the request content
			"Date",           // set by system
			"Host",           // set by system
			"Range",          // handled explicitly
			"Cookie"          // merged with Cookies / CookieContainer in AppendCookies
		};

		private static readonly Regex _rangeRegex = new Regex(@"=(\d+)-(\d+)$", RegexOptions.Compiled);

		private static bool IsBodyMethod(string method) =>
			method == "POST" || method == "PUT" || method == "PATCH";

		/// <summary>
		/// Sends the request using a shared <see cref="HttpClient"/> and maps the result to an <see cref="HttpResponse"/>.
		/// Never throws: transport failures, timeouts and cancellation are reported via <see cref="HttpResponse.ResponseStatus"/>.
		/// </summary>
		protected async Task<HttpResponse> SendRequestAsync(string method, CancellationToken cancellationToken)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };
			var lease = default(HttpClientCache.Lease);

			using (var timeoutCts = new CancellationTokenSource())
			using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
			{
				try
				{
					using (var request = CreateRequestMessage(method))
					{
						lease = HttpClientCache.Acquire(new HttpClientCache.Key(FollowRedirects, MaxRedirects, Proxy, Credentials, ClientCertificates));

						if (Timeout > 0)
							timeoutCts.CancelAfter(Timeout);

						using (var httpResponse = await lease.Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, linkedCts.Token).ConfigureAwait(false))
						{
							await ExtractResponseDataAsync(response, httpResponse).ConfigureAwait(false);
						}
					}
				}
				catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
				{
					response.ResponseStatus = ResponseStatus.Aborted;
					response.ErrorMessage = ex.Message;
					response.ErrorException = ex;
				}
				catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
				{
					var timeout = new TimeoutException($"The request timed out after {Timeout} ms.");
					response.ResponseStatus = ResponseStatus.TimedOut;
					response.ErrorMessage = timeout.Message;
					response.ErrorException = timeout;
				}
				catch (Exception ex)
				{
					response.ResponseStatus = ResponseStatus.Error;
					response.ErrorMessage = ex.Message;
					response.ErrorException = ex;
				}
				finally
				{
					lease.Dispose();
				}
			}

			return response;
		}

		private HttpRequestMessage CreateRequestMessage(string method)
		{
			var request = new HttpRequestMessage(new HttpMethod(method), Url);
			var contentHeaders = new List<HttpHeader>();
			var explicitCookies = new List<string>();

			foreach (var header in Headers)
			{
				if (_ignoredRequestHeaders.Contains(header.Name))
				{
					if (string.Equals(header.Name, "Range", StringComparison.OrdinalIgnoreCase))
						AddRange(request, header.Value);
					else if (string.Equals(header.Name, "Cookie", StringComparison.OrdinalIgnoreCase) && header.Value.HasValue())
						explicitCookies.Add(header.Value);
					continue;
				}

				if (!request.Headers.TryAddWithoutValidation(header.Name, header.Value))
					contentHeaders.Add(header);
			}

			if (UserAgent.HasValue())
			{
				request.Headers.Remove("User-Agent");
				request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
			}

			AppendCookies(request, explicitCookies);

			request.Content = IsBodyMethod(method) ? CreateBodyContent() : null;

			if (request.Content == null && contentHeaders.Count > 0)
				request.Content = new ByteArrayContent(new byte[0]);

			if (request.Content != null)
			{
				foreach (var header in contentHeaders)
				{
					// body-derived Content-Type wins over an explicit header, matching the HttpWebRequest behavior
					if (string.Equals(header.Name, "Content-Type", StringComparison.OrdinalIgnoreCase) && request.Content.Headers.ContentType != null)
						continue;

					request.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
				}
			}

			return request;
		}

		private HttpContent CreateBodyContent()
		{
			HttpContent content;
			string contentType;

			if (HasFiles)
			{
				content = new MultipartFormContent(this);
				contentType = GetMultipartFormContentType();
			}
			else if (HasParameters)
			{
				content = new ByteArrayContent(_defaultEncoding.GetBytes(EncodeParameters()));
				contentType = "application/x-www-form-urlencoded";
			}
			else if (HasBody)
			{
				content = new ByteArrayContent(_defaultEncoding.GetBytes(RequestBody));
				contentType = RequestContentType;
			}
			else
			{
				// make sure Content-Length: 0 is always sent for body methods
				return new ByteArrayContent(new byte[0]);
			}

			if (contentType.HasValue())
				content.Headers.TryAddWithoutValidation("Content-Type", contentType);

			return content;
		}

		private void AppendCookies(HttpRequestMessage request, List<string> explicitCookies)
		{
			var container = CookieContainer ?? new CookieContainer();
			foreach (var httpCookie in Cookies)
			{
				container.Add(new Cookie
				{
					Name = httpCookie.Name,
					Value = httpCookie.Value,
					Domain = Url.Host
				});
			}

			// an explicit Cookie header wins over container cookies with the same name
			var pairs = explicitCookies
				.SelectMany(h => h.Split(';'))
				.Select(c => c.Trim())
				.Where(c => c.Length > 0)
				.ToList();
			var explicitNames = new HashSet<string>(pairs.Select(CookieName), StringComparer.Ordinal);

			pairs.AddRange(container.GetCookies(Url).Cast<Cookie>()
				.Where(c => !explicitNames.Contains(c.Name))
				.Select(c => c.ToString()));

			if (pairs.Count > 0)
				request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", pairs));
		}

		private static string CookieName(string pair)
		{
			var i = pair.IndexOf('=');
			return (i < 0 ? pair : pair.Substring(0, i)).Trim();
		}

		private static void AddRange(HttpRequestMessage request, string range)
		{
			var m = _rangeRegex.Match(range);
			if (!m.Success)
				return;

			request.Headers.Range = new RangeHeaderValue(Convert.ToInt64(m.Groups[1].Value), Convert.ToInt64(m.Groups[2].Value));
		}

		private async Task ExtractResponseDataAsync(HttpResponse response, HttpResponseMessage httpResponse)
		{
			var content = httpResponse.Content;

			response.RawBytes = content != null
				? await content.ReadAsByteArrayAsync().ConfigureAwait(false)
				: new byte[0];

			response.ContentEncoding = content != null ? string.Join(", ", content.Headers.ContentEncoding) : string.Empty;
			response.ContentType = GetHeaderValue(content?.Headers, "Content-Type");
			response.ContentLength = content?.Headers.ContentLength ?? -1;
			response.Server = httpResponse.Headers.Server.ToString();
			response.StatusCode = httpResponse.StatusCode;
			response.StatusDescription = httpResponse.ReasonPhrase;
			response.ResponseUri = httpResponse.RequestMessage?.RequestUri ?? Url;
			response.ResponseStatus = ResponseStatus.Completed;

			if (httpResponse.Headers.TryGetValues("Set-Cookie", out var setCookies))
			{
				foreach (var setCookie in setCookies)
					ProcessSetCookie(response, response.ResponseUri, setCookie);
			}

			var allHeaders = content == null
				? httpResponse.Headers
				: httpResponse.Headers.Concat(content.Headers);

			foreach (var header in allHeaders)
				response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(", ", header.Value) });
		}

		private void ProcessSetCookie(HttpResponse response, Uri responseUri, string setCookie)
		{
			try
			{
				CookieContainer?.SetCookies(responseUri, setCookie);
			}
			catch (CookieException)
			{
			}

			foreach (var cookie in ParseSetCookie(responseUri, setCookie))
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

		private static readonly Regex _cookiePathRegex = new Regex(@";\s*path\s*=\s*([^;]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Parses a single Set-Cookie header value with the BCL cookie parser (netstandard2.0 has no public
		/// Set-Cookie parser, so round-trip it through a throwaway container scoped to the cookie's own path).
		/// </summary>
		private static IEnumerable<Cookie> ParseSetCookie(Uri responseUri, string setCookie)
		{
			var container = new CookieContainer();
			try
			{
				container.SetCookies(responseUri, setCookie);
			}
			catch (CookieException)
			{
				return Enumerable.Empty<Cookie>();
			}

			var pathMatch = _cookiePathRegex.Match(setCookie);
			var path = pathMatch.Success && pathMatch.Groups[1].Value.Trim().StartsWith("/")
				? pathMatch.Groups[1].Value.Trim()
				: responseUri.AbsolutePath;

			// https so that Secure cookies are returned as well
			var lookupUri = new UriBuilder(responseUri) { Scheme = Uri.UriSchemeHttps, Port = -1, Path = path, Query = string.Empty }.Uri;
			return container.GetCookies(lookupUri).Cast<Cookie>().ToList();
		}

		private static string GetHeaderValue(HttpHeaders headers, string name)
		{
			if (headers != null && headers.TryGetValues(name, out var values))
				return string.Join(", ", values);
			return string.Empty;
		}

		/// <summary>
		/// Streams the multipart body with the same wire format (and Content-Length) as the HttpWebRequest implementation,
		/// writing file data directly via <see cref="HttpFile.Writer"/>.
		/// </summary>
		private sealed class MultipartFormContent : HttpContent
		{
			private readonly Http _http;

			public MultipartFormContent(Http http)
			{
				_http = http;
			}

			protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
			{
				_http.WriteMultipartFormData(stream);
				return Task.CompletedTask;
			}

			protected override bool TryComputeLength(out long length)
			{
				// files added via a Writer without a known length are sent chunked
				if (_http.Files.Any(f => !f.ContentLengthSpecified))
				{
					length = 0;
					return false;
				}

				length = _http.CalculateContentLength();
				return true;
			}
		}

		private long CalculateContentLength()
		{
			long length = 0;
			foreach (var file in Files)
			{
				length += _defaultEncoding.GetByteCount(GetMultipartFileHeader(file));
				length += file.ContentLength;
				length += _defaultEncoding.GetByteCount(_lineBreak);
			}

			foreach (var param in Parameters)
			{
				length += _defaultEncoding.GetByteCount(GetMultipartFormData(param));
			}

			length += _defaultEncoding.GetByteCount(GetMultipartFooter());
			return length;
		}
	}
}
