using Xunit;

namespace RestSharp.Tests
{
	public class ParameterTests
	{
		[Fact]
		public void Properties_Can_Be_Set()
		{
			var p = new Parameter();
			p.Name = "test";
			p.Value = "value";
			p.Type = ParameterType.GetOrPost;

			Assert.Equal("test", p.Name);
			Assert.Equal("value", p.Value);
			Assert.Equal(ParameterType.GetOrPost, p.Type);
		}

		[Fact]
		public void ToString_Returns_Name_Value_Pair()
		{
			var p = new Parameter { Name = "key", Value = "val", Type = ParameterType.GetOrPost };
			var str = p.ToString();
			Assert.Contains("key", str);
			Assert.Contains("val", str);
		}

		[Fact]
		public void ToString_With_Null_Value()
		{
			var p = new Parameter { Name = "key", Value = null, Type = ParameterType.Cookie };
			var str = p.ToString();
			Assert.Contains("key", str);
		}

		[Fact]
		public void All_Parameter_Types()
		{
			Assert.Equal(ParameterType.Cookie, (ParameterType)0);
			Assert.Equal(ParameterType.GetOrPost, (ParameterType)1);
			Assert.Equal(ParameterType.UrlSegment, (ParameterType)2);
			Assert.Equal(ParameterType.HttpHeader, (ParameterType)3);
			Assert.Equal(ParameterType.RequestBody, (ParameterType)4);
		}

		[Fact]
		public void HttpHeader_Properties()
		{
			var h = new HttpHeader();
			h.Name = "Content-Type";
			h.Value = "application/json";

			Assert.Equal("Content-Type", h.Name);
			Assert.Equal("application/json", h.Value);
		}

		[Fact]
		public void HttpParameter_Properties()
		{
			var p = new HttpParameter();
			p.Name = "key";
			p.Value = "val";

			Assert.Equal("key", p.Name);
			Assert.Equal("val", p.Value);
		}

		[Fact]
		public void HttpCookie_Properties()
		{
			var c = new HttpCookie();
			c.Name = "session";
			c.Value = "abc123";

			Assert.Equal("session", c.Name);
			Assert.Equal("abc123", c.Value);
		}

		[Fact]
		public void HttpFile_Properties()
		{
			var f = new HttpFile();
			f.Name = "file";
			f.FileName = "test.txt";
			f.ContentType = "text/plain";
			f.ContentLength = 100;
			f.Writer = s => { };

			Assert.Equal("file", f.Name);
			Assert.Equal("test.txt", f.FileName);
			Assert.Equal("text/plain", f.ContentType);
			Assert.Equal(100, f.ContentLength);
			Assert.NotNull(f.Writer);
		}
	}
}
