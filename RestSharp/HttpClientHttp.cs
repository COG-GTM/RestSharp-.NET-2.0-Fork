using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp.Extensions;

namespace RestSharp
{
	/// <summary>
	/// </summary>
	public class HttpClientHttp : IHttp, IHttpFactory
	{
		private static readonly HttpClient _defaultHttpClient = new HttpClient();
		private readonly HttpClient _httpClient;
		private const string _lineBreak = "\r\n";
		private static readonly Encoding _defaultEncoding = Encoding.UTF8;

		public HttpClientHttp() : this(_defaultHttpClient)
		{
		}

		public HttpClientHttp(HttpClient httpClient)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Headers = new List<HttpHeader>();
			Files = new List<HttpFile>();
			Parameters = new List<HttpParameter>();
			Cookies = new List<HttpCookie>();
		}

		///<summary>
		/// Creates an IHttp
		///</summary>
		///<returns></returns>
		public IHttp Create()
		{
			return new HttpClientHttp();
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

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action)
		{
			_ = DeleteAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest GetAsync(Action<HttpResponse> action)
		{
			_ = GetAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest HeadAsync(Action<HttpResponse> action)
		{
			_ = HeadAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest OptionsAsync(Action<HttpResponse> action)
		{
			_ = OptionsAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest PostAsync(Action<HttpResponse> action)
		{
			_ = PostAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest PutAsync(Action<HttpResponse> action)
		{
			_ = PutAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		public HttpWebRequest PatchAsync(Action<HttpResponse> action)
		{
			_ = PatchAsyncInternal(action);
			return null; // HttpClient doesn't return HttpWebRequest
		}

		private async Task DeleteAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Delete);
			callback(response);
		}

		private async Task GetAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Get);
			callback(response);
		}

		private async Task HeadAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Head);
			callback(response);
		}

		private async Task OptionsAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Options);
			callback(response);
		}

		private async Task PostAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Post);
			callback(response);
		}

		private async Task PutAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(HttpMethod.Put);
			callback(response);
		}

		private async Task PatchAsyncInternal(Action<HttpResponse> callback)
		{
			var response = await ExecuteAsync(new HttpMethod("PATCH"));
			callback(response);
		}

		public HttpResponse Delete()
		{
			return ExecuteAsync(HttpMethod.Delete).GetAwaiter().GetResult();
		}

		public HttpResponse Get()
		{
			return ExecuteAsync(HttpMethod.Get).GetAwaiter().GetResult();
		}

		public HttpResponse Head()
		{
			return ExecuteAsync(HttpMethod.Head).GetAwaiter().GetResult();
		}

		public HttpResponse Options()
		{
			return ExecuteAsync(HttpMethod.Options).GetAwaiter().GetResult();
		}

		public HttpResponse Post()
		{
			return ExecuteAsync(HttpMethod.Post).GetAwaiter().GetResult();
		}

		public HttpResponse Put()
		{
			return ExecuteAsync(HttpMethod.Put).GetAwaiter().GetResult();
		}

		public HttpResponse Patch()
		{
			return ExecuteAsync(new HttpMethod("PATCH")).GetAwaiter().GetResult();
		}

		private async Task<HttpResponse> ExecuteAsync(HttpMethod method)
		{
			var response = new HttpResponse { ResponseStatus = ResponseStatus.None };

			try
			{
				using (var request = CreateHttpRequestMessage(method))
				using (var cancellationTokenSource = Timeout > 0 ? new CancellationTokenSource(Timeout) : new CancellationTokenSource())
				using (var httpResponse = await _httpClient.SendAsync(request, cancellationTokenSource.Token))
				{
					await ExtractResponseData(response, httpResponse);
				}
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				response.ResponseStatus = ResponseStatus.TimedOut;
				response.ErrorMessage = "Request timed out";
				response.ErrorException = ex;
			}
			catch (TaskCanceledException ex)
			{
				response.ResponseStatus = ResponseStatus.Aborted;
				response.ErrorMessage = "Request was cancelled";
				response.ErrorException = ex;
			}
			catch (Exception ex)
			{
				response.ResponseStatus = ResponseStatus.Error;
				response.ErrorMessage = ex.Message;
				response.ErrorException = ex;
			}

			return response;
		}

		private HttpRequestMessage CreateHttpRequestMessage(HttpMethod method)
		{
			var request = new HttpRequestMessage(method, Url);

			foreach (var header in Headers)
			{
				if (IsContentHeader(header.Name))
				{
					continue;
				}
				request.Headers.TryAddWithoutValidation(header.Name, header.Value);
			}

			if (!string.IsNullOrEmpty(UserAgent))
			{
				request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
			}

			if (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH")
			{
				request.Content = CreateHttpContent();
			}

			return request;
		}

		private HttpContent CreateHttpContent()
		{
			if (Files.Any())
			{
				return CreateMultipartContent();
			}
			else if (Parameters.Any())
			{
				return CreateFormUrlEncodedContent();
			}
			else if (!string.IsNullOrEmpty(RequestBody))
			{
				return CreateStringContent();
			}

			return null;
		}

		private HttpContent CreateMultipartContent()
		{
			var multipartContent = new MultipartFormDataContent();

			foreach (var param in Parameters)
			{
				multipartContent.Add(new StringContent(param.Value ?? string.Empty), param.Name);
			}

			foreach (var file in Files)
			{
				var fileContent = new StreamContent(new MemoryStream());
				file.Writer(fileContent.ReadAsStreamAsync().GetAwaiter().GetResult());
				fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
				multipartContent.Add(fileContent, file.Name, file.FileName);
			}

			return multipartContent;
		}

		private HttpContent CreateFormUrlEncodedContent()
		{
			var formData = Parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value ?? string.Empty));
			return new FormUrlEncodedContent(formData);
		}

		private HttpContent CreateStringContent()
		{
			var content = new StringContent(RequestBody, _defaultEncoding, RequestContentType ?? "text/plain");
			
			foreach (var header in Headers.Where(h => IsContentHeader(h.Name)))
			{
				content.Headers.TryAddWithoutValidation(header.Name, header.Value);
			}

			return content;
		}

		private static bool IsContentHeader(string headerName)
		{
			return headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
				   headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
				   headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
				   headerName.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase);
		}

		private static async Task ExtractResponseData(HttpResponse response, HttpResponseMessage httpResponse)
		{
			response.StatusCode = httpResponse.StatusCode;
			response.StatusDescription = httpResponse.ReasonPhrase;
			response.ResponseUri = httpResponse.RequestMessage?.RequestUri;
			response.ContentType = httpResponse.Content?.Headers?.ContentType?.ToString();
			response.ContentLength = httpResponse.Content?.Headers?.ContentLength ?? 0;
			response.ResponseStatus = ResponseStatus.Completed;

			if (httpResponse.Content != null)
			{
				response.RawBytes = await httpResponse.Content.ReadAsByteArrayAsync();
			}

			foreach (var header in httpResponse.Headers)
			{
				response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(", ", header.Value) });
			}

			if (httpResponse.Content?.Headers != null)
			{
				foreach (var header in httpResponse.Content.Headers)
				{
					response.Headers.Add(new HttpHeader { Name = header.Key, Value = string.Join(", ", header.Value) });
				}
			}

		}
	}
}
