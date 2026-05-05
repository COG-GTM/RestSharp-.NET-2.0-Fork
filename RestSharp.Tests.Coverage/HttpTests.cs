using System;
using System.Linq;
using System.Net;
using Xunit;

namespace RestSharp.Tests
{
	public class HttpTests
	{
		[Fact]
		public void Default_Constructor_Initializes_Collections()
		{
			var http = new Http();
			Assert.NotNull(http.Headers);
			Assert.Empty(http.Headers);
			Assert.NotNull(http.Parameters);
			Assert.Empty(http.Parameters);
			Assert.NotNull(http.Cookies);
			Assert.Empty(http.Cookies);
			Assert.NotNull(http.Files);
			Assert.Empty(http.Files);
		}

		[Fact]
		public void HasParameters_False_By_Default()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasParameters);
		}

		[Fact]
		public void HasParameters_True_When_Parameters_Added()
		{
			var http = new TestableHttp();
			http.Parameters.Add(new HttpParameter { Name = "key", Value = "val" });
			Assert.True(http.TestHasParameters);
		}

		[Fact]
		public void HasCookies_False_By_Default()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasCookies);
		}

		[Fact]
		public void HasCookies_True_When_Cookies_Added()
		{
			var http = new TestableHttp();
			http.Cookies.Add(new HttpCookie { Name = "session", Value = "abc" });
			Assert.True(http.TestHasCookies);
		}

		[Fact]
		public void HasBody_False_By_Default()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasBody);
		}

		[Fact]
		public void HasBody_True_When_RequestBody_Set()
		{
			var http = new TestableHttp();
			http.RequestBody = "body content";
			Assert.True(http.TestHasBody);
		}

		[Fact]
		public void HasFiles_False_By_Default()
		{
			var http = new TestableHttp();
			Assert.False(http.TestHasFiles);
		}

		[Fact]
		public void HasFiles_True_When_Files_Added()
		{
			var http = new TestableHttp();
			http.Files.Add(new HttpFile { Name = "file", FileName = "test.txt", Writer = s => { } });
			Assert.True(http.TestHasFiles);
		}

		[Fact]
		public void UserAgent_Can_Be_Set()
		{
			var http = new Http();
			http.UserAgent = "TestAgent/1.0";
			Assert.Equal("TestAgent/1.0", http.UserAgent);
		}

		[Fact]
		public void Timeout_Can_Be_Set()
		{
			var http = new Http();
			http.Timeout = 5000;
			Assert.Equal(5000, http.Timeout);
		}

		[Fact]
		public void Url_Can_Be_Set()
		{
			var http = new Http();
			http.Url = new Uri("http://example.com");
			Assert.Equal("http://example.com/", http.Url.ToString());
		}

		[Fact]
		public void RequestBody_Can_Be_Set()
		{
			var http = new Http();
			http.RequestBody = "test body";
			Assert.Equal("test body", http.RequestBody);
		}

		[Fact]
		public void RequestContentType_Can_Be_Set()
		{
			var http = new Http();
			http.RequestContentType = "application/json";
			Assert.Equal("application/json", http.RequestContentType);
		}

		[Fact]
		public void Credentials_Can_Be_Set()
		{
			var http = new Http();
			http.Credentials = new NetworkCredential("user", "pass");
			Assert.NotNull(http.Credentials);
		}

		[Fact]
		public void CookieContainer_Can_Be_Set()
		{
			var http = new Http();
			var container = new CookieContainer();
			http.CookieContainer = container;
			Assert.Same(container, http.CookieContainer);
		}

		[Fact]
		public void FollowRedirects_Can_Be_Set()
		{
			var http = new Http();
			http.FollowRedirects = true;
			Assert.True(http.FollowRedirects);
		}

		[Fact]
		public void MaxRedirects_Can_Be_Set()
		{
			var http = new Http();
			http.MaxRedirects = 10;
			Assert.Equal(10, http.MaxRedirects);
		}

		[Fact]
		public void Proxy_Can_Be_Set()
		{
			var http = new Http();
			var proxy = new WebProxy("http://proxy.local");
			http.Proxy = proxy;
			Assert.Same(proxy, http.Proxy);
		}

		[Fact]
		public void IHttpFactory_Create_Returns_Http()
		{
			var http = new Http();
			var created = ((IHttpFactory)http).Create();
			Assert.NotNull(created);
			Assert.IsType<Http>(created);
		}

		private class TestableHttp : Http
		{
			public bool TestHasParameters => HasParameters;
			public bool TestHasCookies => HasCookies;
			public bool TestHasBody => HasBody;
			public bool TestHasFiles => HasFiles;
		}
	}
}
