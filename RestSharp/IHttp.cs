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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RestSharp
{
	public interface IHttp
	{
		CookieContainer CookieContainer { get; set; }
		ICredentials Credentials { get; set; }
		string UserAgent { get; set; }
		int Timeout { get; set; }
		bool FollowRedirects { get; set; }
		X509CertificateCollection ClientCertificates { get; set; }
		int? MaxRedirects { get; set; }

		IList<HttpHeader> Headers { get; }
		IList<HttpParameter> Parameters { get; }
		IList<HttpFile> Files { get; }
		IList<HttpCookie> Cookies { get; }
		string RequestBody { get; set; }
		string RequestContentType { get; set; }

		Uri Url { get; set; }

		Task<HttpResponse> DeleteAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> GetAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> HeadAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> OptionsAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> PostAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> PutAsync(CancellationToken cancellationToken = default(CancellationToken));
		Task<HttpResponse> PatchAsync(CancellationToken cancellationToken = default(CancellationToken));

		IWebProxy Proxy { get; set; }
	}
}
