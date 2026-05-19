using System;
using System.Linq;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators.OAuth.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class OAuthToolsTests
	{
		[Fact]
		public void GetNonce_Returns_16_Character_String()
		{
			var nonce = OAuthTools.GetNonce();
			Assert.Equal(16, nonce.Length);
		}

		[Fact]
		public void GetNonce_Contains_Only_Lowercase_And_Digits()
		{
			var nonce = OAuthTools.GetNonce();
			Assert.True(nonce.All(c => char.IsLower(c) || char.IsDigit(c)));
		}

		[Fact]
		public void GetNonce_Returns_Unique_Values()
		{
			var nonce1 = OAuthTools.GetNonce();
			var nonce2 = OAuthTools.GetNonce();
			Assert.NotEqual(nonce1, nonce2);
		}

		[Fact]
		public void GetTimestamp_Returns_Numeric_String()
		{
			var timestamp = OAuthTools.GetTimestamp();
			long result;
			Assert.True(long.TryParse(timestamp, out result));
		}

		[Fact]
		public void GetTimestamp_For_Known_Date()
		{
			var date = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var timestamp = OAuthTools.GetTimestamp(date);
			var expected = (long)(date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
			Assert.Equal(expected.ToString(), timestamp);
		}

		[Fact]
		public void UrlEncodeRelaxed_Encodes_Special_Characters()
		{
			var result = OAuthTools.UrlEncodeRelaxed("hello world&foo=bar");
			Assert.Equal("hello%20world%26foo%3Dbar", result);
		}

		[Fact]
		public void UrlEncodeRelaxed_Does_Not_Encode_Unreserved()
		{
			var result = OAuthTools.UrlEncodeRelaxed("abc123");
			Assert.Equal("abc123", result);
		}

		[Fact]
		public void UrlEncodeStrict_Encodes_Apostrophe()
		{
			var result = OAuthTools.UrlEncodeStrict("it's");
			Assert.Contains("%", result);
		}

		[Fact]
		public void NormalizeRequestParameters_Sorts_And_Concatenates()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("z_param", "value2");
			parameters.Add("a_param", "value1");

			var result = OAuthTools.NormalizeRequestParameters(parameters);
			Assert.True(result.IndexOf("a_param") < result.IndexOf("z_param"));
		}

		[Fact]
		public void NormalizeRequestParameters_Excludes_Signature()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("oauth_signature", "sig_value");
			parameters.Add("oauth_token", "token_value");

			var result = OAuthTools.NormalizeRequestParameters(parameters);
			Assert.DoesNotContain("sig_value", result);
			Assert.Contains("oauth_token", result);
		}

		[Fact]
		public void ConcatenateRequestElements_Builds_Signature_Base()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("oauth_consumer_key", "key");

			var result = OAuthTools.ConcatenateRequestElements("GET", "http://example.com/resource", parameters);

			Assert.True(result.StartsWith("GET&"));
			Assert.Contains("example.com", result);
		}

		[Fact]
		public void GetSignature_HmacSha1_Returns_Non_Empty()
		{
			var signatureBase = "GET&http%3A%2F%2Fexample.com&oauth_consumer_key%3Dkey";
			var result = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				"consumer_secret",
				"token_secret");

			Assert.False(string.IsNullOrEmpty(result));
		}

		[Fact]
		public void GetSignature_HmacSha1_Without_TokenSecret()
		{
			var signatureBase = "GET&http%3A%2F%2Fexample.com&param%3Dvalue";
			var result = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				signatureBase,
				"consumer_secret");

			Assert.False(string.IsNullOrEmpty(result));
		}

		[Fact]
		public void GetSignature_PlainText_Throws_NotImplemented()
		{
			Assert.Throws<NotImplementedException>(() =>
				OAuthTools.GetSignature(
					OAuthSignatureMethod.PlainText,
					OAuthSignatureTreatment.Escaped,
					"base",
					"secret",
					"token"));
		}

		[Fact]
		public void ConstructRequestUrl_Strips_Default_Port_80()
		{
			var url = new Uri("http://example.com:80/path");
			var result = OAuthTools.ConstructRequestUrl(url);
			Assert.DoesNotContain(":80", result);
			Assert.Contains("/path", result);
		}

		[Fact]
		public void ConstructRequestUrl_Strips_Default_Port_443()
		{
			var url = new Uri("https://example.com:443/path");
			var result = OAuthTools.ConstructRequestUrl(url);
			Assert.DoesNotContain(":443", result);
		}

		[Fact]
		public void ConstructRequestUrl_Keeps_Non_Default_Port()
		{
			var url = new Uri("http://example.com:8080/path");
			var result = OAuthTools.ConstructRequestUrl(url);
			Assert.Contains(":8080", result);
		}

		[Fact]
		public void ConstructRequestUrl_Throws_For_Null()
		{
			Assert.Throws<ArgumentNullException>(() => OAuthTools.ConstructRequestUrl(null));
		}

		[Fact]
		public void GetSignature_Escaped_Treatment()
		{
			var signatureBase = "GET&http%3A%2F%2Fexample.com&param%3Dvalue";
			var escaped = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				signatureBase,
				"secret",
				"token");

			var unescaped = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Unescaped,
				signatureBase,
				"secret",
				"token");

			Assert.NotEqual(escaped, unescaped);
		}
	}
}
