using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace RestSharp.Tests.Fakes
{
	public class FakeHttp : IHttp
	{
		public FakeHttp()
		{
			Headers = new List<HttpHeader>();
			Parameters = new List<HttpParameter>();
			Files = new List<HttpFile>();
			Cookies = new List<HttpCookie>();
			CannedResponse = new HttpResponse();
		}

		public HttpResponse CannedResponse { get; set; }
		public string LastMethod { get; private set; }

		public CookieContainer CookieContainer { get; set; }
		public ICredentials Credentials { get; set; }
		public string UserAgent { get; set; }
		public int Timeout { get; set; }
		public bool FollowRedirects { get; set; }
		public X509CertificateCollection ClientCertificates { get; set; }
		public int? MaxRedirects { get; set; }
		public IList<HttpHeader> Headers { get; private set; }
		public IList<HttpParameter> Parameters { get; private set; }
		public IList<HttpFile> Files { get; private set; }
		public IList<HttpCookie> Cookies { get; private set; }
		public string RequestBody { get; set; }
		public string RequestContentType { get; set; }
		public Uri Url { get; set; }
		public IWebProxy Proxy { get; set; }

		public HttpWebRequest DeleteAsync(Action<HttpResponse> action) { LastMethod = "DELETE"; action(CannedResponse); return null; }
		public HttpWebRequest GetAsync(Action<HttpResponse> action) { LastMethod = "GET"; action(CannedResponse); return null; }
		public HttpWebRequest HeadAsync(Action<HttpResponse> action) { LastMethod = "HEAD"; action(CannedResponse); return null; }
		public HttpWebRequest OptionsAsync(Action<HttpResponse> action) { LastMethod = "OPTIONS"; action(CannedResponse); return null; }
		public HttpWebRequest PostAsync(Action<HttpResponse> action) { LastMethod = "POST"; action(CannedResponse); return null; }
		public HttpWebRequest PutAsync(Action<HttpResponse> action) { LastMethod = "PUT"; action(CannedResponse); return null; }
		public HttpWebRequest PatchAsync(Action<HttpResponse> action) { LastMethod = "PATCH"; action(CannedResponse); return null; }

		public HttpResponse Delete() { LastMethod = "DELETE"; return CannedResponse; }
		public HttpResponse Get() { LastMethod = "GET"; return CannedResponse; }
		public HttpResponse Head() { LastMethod = "HEAD"; return CannedResponse; }
		public HttpResponse Options() { LastMethod = "OPTIONS"; return CannedResponse; }
		public HttpResponse Post() { LastMethod = "POST"; return CannedResponse; }
		public HttpResponse Put() { LastMethod = "PUT"; return CannedResponse; }
		public HttpResponse Patch() { LastMethod = "PATCH"; return CannedResponse; }
	}
}
