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
	}
}
