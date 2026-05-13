using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RestSharp;
using Xunit;

namespace RestSharp.Tests.Coverage
{
	public class HttpTests
	{
		[Fact]
		public void Http_Default_Constructor_Initializes_Collections()
		{
			var http = new Http();
			Assert.NotNull(http.Headers);
			Assert.NotNull(http.Files);
			Assert.NotNull(http.Parameters);
			Assert.NotNull(http.Cookies);
		}

		[Fact]
		public void Http_UserAgent_Can_Be_Set()
		{
			var http = new Http();
			http.UserAgent = "Test/1.0";
			Assert.Equal("Test/1.0", http.UserAgent);
		}

		[Fact]
		public void Http_Timeout_Can_Be_Set()
		{
			var http = new Http();
			http.Timeout = 5000;
			Assert.Equal(5000, http.Timeout);
		}

		[Fact]
		public void Http_Credentials_Can_Be_Set()
		{
			var http = new Http();
			var creds = new NetworkCredential("user", "pass");
			http.Credentials = creds;
			Assert.Equal(creds, http.Credentials);
		}

		[Fact]
		public void Http_CookieContainer_Can_Be_Set()
		{
			var http = new Http();
			var container = new CookieContainer();
			http.CookieContainer = container;
			Assert.Equal(container, http.CookieContainer);
		}

		[Fact]
		public void Http_FollowRedirects_Can_Be_Set()
		{
			var http = new Http();
			http.FollowRedirects = true;
			Assert.True(http.FollowRedirects);
		}

		[Fact]
		public void Http_MaxRedirects_Can_Be_Set()
		{
			var http = new Http();
			http.MaxRedirects = 5;
			Assert.Equal(5, http.MaxRedirects);
		}

		[Fact]
		public void Http_Url_Can_Be_Set()
		{
			var http = new Http();
			http.Url = new Uri("http://example.com");
			Assert.Equal("http://example.com/", http.Url.ToString());
		}

		[Fact]
		public void Http_RequestBody_Can_Be_Set()
		{
			var http = new Http();
			http.RequestBody = "body content";
			Assert.Equal("body content", http.RequestBody);
		}

		[Fact]
		public void Http_RequestContentType_Can_Be_Set()
		{
			var http = new Http();
			http.RequestContentType = "application/json";
			Assert.Equal("application/json", http.RequestContentType);
		}

		[Fact]
		public void Http_ClientCertificates_Can_Be_Set()
		{
			var http = new Http();
			var certs = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
			http.ClientCertificates = certs;
			Assert.Equal(certs, http.ClientCertificates);
		}

		[Fact]
		public void Http_Proxy_Can_Be_Set()
		{
			var http = new Http();
			var proxy = new WebProxy("http://proxy.example.com:8080");
			http.Proxy = proxy;
			Assert.Equal(proxy, http.Proxy);
		}

		[Fact]
		public void Http_Create_Returns_New_Instance()
		{
			var http = new Http();
			var created = http.Create();
			Assert.NotNull(created);
			Assert.IsType<Http>(created);
		}

		[Fact]
		public void Http_HasParameters_Returns_False_When_Empty()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasParameters);
		}

		[Fact]
		public void Http_HasParameters_Returns_True_When_Parameters_Exist()
		{
			var http = new TestableHttp();
			http.Parameters.Add(new HttpParameter { Name = "q", Value = "test" });
			Assert.True(http.TestHasParameters);
		}

		[Fact]
		public void Http_HasCookies_Returns_False_When_Empty()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasCookies);
		}

		[Fact]
		public void Http_HasCookies_Returns_True_When_Cookies_Exist()
		{
			var http = new TestableHttp();
			http.Cookies.Add(new HttpCookie { Name = "session", Value = "abc" });
			Assert.True(http.TestHasCookies);
		}

		[Fact]
		public void Http_HasBody_Returns_False_When_Empty()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasBody);
		}

		[Fact]
		public void Http_HasBody_Returns_True_When_Body_Set()
		{
			var http = new TestableHttp();
			http.RequestBody = "some body";
			Assert.True(http.TestHasBody);
		}

		[Fact]
		public void Http_HasFiles_Returns_False_When_Empty()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasFiles);
		}

		[Fact]
		public void Http_HasFiles_Returns_True_When_Files_Exist()
		{
			var http = new TestableHttp();
			http.Files.Add(new HttpFile { Name = "file", FileName = "test.txt" });
			Assert.True(http.TestHasFiles);
		}

		[Fact]
		public void Http_Headers_Collection_Can_Be_Modified()
		{
			var http = new Http();
			http.Headers.Add(new HttpHeader { Name = "X-Test", Value = "123" });
			Assert.Single(http.Headers);
			Assert.Equal("X-Test", http.Headers[0].Name);
		}

		[Fact]
		public void Http_Parameters_Collection_Can_Be_Modified()
		{
			var http = new Http();
			http.Parameters.Add(new HttpParameter { Name = "key", Value = "val" });
			Assert.Single(http.Parameters);
		}

		[Fact]
		public void Http_Cookies_Collection_Can_Be_Modified()
		{
			var http = new Http();
			http.Cookies.Add(new HttpCookie { Name = "c", Value = "v" });
			Assert.Single(http.Cookies);
		}

		[Fact]
		public void Http_Files_Collection_Can_Be_Modified()
		{
			var http = new Http();
			http.Files.Add(new HttpFile { Name = "f" });
			Assert.Single(http.Files);
		}
	}

	public class TestableHttp : Http
	{
		public bool TestHasParameters => HasParameters;
		public bool TestHasCookies => HasCookies;
		public bool TestHasBody => HasBody;
		public bool TestHasFiles => HasFiles;
	}

	public class IHttpFactoryTests
	{
		[Fact]
		public void SimpleFactory_Creates_Instance()
		{
			var factory = new SimpleFactory<Http>();
			var http = factory.Create();
			Assert.NotNull(http);
			Assert.IsType<Http>(http);
		}
	}
}
