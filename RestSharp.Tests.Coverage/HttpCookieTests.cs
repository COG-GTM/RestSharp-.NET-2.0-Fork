using System;
using Xunit;

namespace RestSharp.Tests
{
	public class HttpCookieTests
	{
		[Fact]
		public void All_Properties_Can_Be_Set()
		{
			var cookie = new HttpCookie();
			var now = DateTime.Now;

			cookie.Comment = "test comment";
			cookie.CommentUri = new Uri("http://example.com");
			cookie.Discard = true;
			cookie.Domain = ".example.com";
			cookie.Expired = false;
			cookie.Expires = now;
			cookie.HttpOnly = true;
			cookie.Name = "session";
			cookie.Path = "/";
			cookie.Port = "80";
			cookie.Secure = true;
			cookie.TimeStamp = now;
			cookie.Value = "abc123";
			cookie.Version = 2;

			Assert.Equal("test comment", cookie.Comment);
			Assert.Equal("http://example.com/", cookie.CommentUri.ToString());
			Assert.True(cookie.Discard);
			Assert.Equal(".example.com", cookie.Domain);
			Assert.False(cookie.Expired);
			Assert.Equal(now, cookie.Expires);
			Assert.True(cookie.HttpOnly);
			Assert.Equal("session", cookie.Name);
			Assert.Equal("/", cookie.Path);
			Assert.Equal("80", cookie.Port);
			Assert.True(cookie.Secure);
			Assert.Equal(now, cookie.TimeStamp);
			Assert.Equal("abc123", cookie.Value);
			Assert.Equal(2, cookie.Version);
		}
	}
}
