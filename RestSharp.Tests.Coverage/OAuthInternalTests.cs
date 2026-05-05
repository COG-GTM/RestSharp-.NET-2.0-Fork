using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using RestSharp.Authenticators.OAuth;
using RestSharp.Authenticators.OAuth.Extensions;
using Xunit;

namespace RestSharp.Tests
{
	public class OAuthInternalTests
	{
		// OAuthTools tests
		[Fact]
		public void OAuthTools_GetNonce_Returns_16_Char_String()
		{
			var nonce = OAuthTools.GetNonce();
			Assert.Equal(16, nonce.Length);
			Assert.True(nonce.All(c => char.IsLetterOrDigit(c)));
		}

		[Fact]
		public void OAuthTools_GetNonce_Returns_Unique_Values()
		{
			var nonce1 = OAuthTools.GetNonce();
			var nonce2 = OAuthTools.GetNonce();
			Assert.NotEqual(nonce1, nonce2);
		}

		[Fact]
		public void OAuthTools_GetTimestamp_Returns_Positive_Number()
		{
			var ts = OAuthTools.GetTimestamp();
			Assert.True(long.Parse(ts) > 0);
		}

		[Fact]
		public void OAuthTools_GetTimestamp_With_DateTime()
		{
			var ts = OAuthTools.GetTimestamp(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			Assert.Equal("1577836800", ts);
		}

		[Fact]
		public void OAuthTools_UrlEncodeRelaxed()
		{
			Assert.Equal("hello%20world", OAuthTools.UrlEncodeRelaxed("hello world"));
		}

		[Fact]
		public void OAuthTools_UrlEncodeStrict()
		{
			var result = OAuthTools.UrlEncodeStrict("hello world");
			Assert.DoesNotContain(" ", result);
		}

		[Fact]
		public void OAuthTools_ConstructRequestUrl_Http_Port80()
		{
			var url = OAuthTools.ConstructRequestUrl(new Uri("http://example.com:80/api/test"));
			Assert.Equal("http://example.com/api/test", url);
		}

		[Fact]
		public void OAuthTools_ConstructRequestUrl_Https_Port443()
		{
			var url = OAuthTools.ConstructRequestUrl(new Uri("https://example.com:443/api/test"));
			Assert.Equal("https://example.com/api/test", url);
		}

		[Fact]
		public void OAuthTools_ConstructRequestUrl_NonStandard_Port()
		{
			var url = OAuthTools.ConstructRequestUrl(new Uri("http://example.com:8080/api"));
			Assert.Equal("http://example.com:8080/api", url);
		}

		[Fact]
		public void OAuthTools_ConstructRequestUrl_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => OAuthTools.ConstructRequestUrl(null));
		}

		[Fact]
		public void OAuthTools_NormalizeRequestParameters()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("b", "2");
			parameters.Add("a", "1");
			var result = OAuthTools.NormalizeRequestParameters(parameters);
			Assert.Contains("a", result);
			Assert.Contains("b", result);
		}

		[Fact]
		public void OAuthTools_SortParametersExcludingSignature()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("z_param", "3");
			parameters.Add("a_param", "1");
			parameters.Add("oauth_signature", "sig_value");
			var sorted = OAuthTools.SortParametersExcludingSignature(parameters);
			Assert.DoesNotContain(sorted, p => p.Name == "oauth_signature");
		}

		[Fact]
		public void OAuthTools_ConcatenateRequestElements()
		{
			var parameters = new WebParameterCollection();
			parameters.Add("oauth_consumer_key", "key");
			var result = OAuthTools.ConcatenateRequestElements("GET", "http://example.com/api", parameters);
			Assert.StartsWith("GET&", result);
		}

		[Fact]
		public void OAuthTools_GetSignature_HmacSha1()
		{
			var sig = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Escaped,
				"base_string",
				"consumer_secret",
				"token_secret");
			Assert.False(string.IsNullOrEmpty(sig));
		}

		[Fact]
		public void OAuthTools_GetSignature_PlainText_Throws()
		{
			Assert.Throws<NotImplementedException>(() => OAuthTools.GetSignature(
				OAuthSignatureMethod.PlainText,
				OAuthSignatureTreatment.Escaped,
				"base_string",
				"consumer_secret",
				"token_secret"));
		}

		[Fact]
		public void OAuthTools_GetSignature_Without_TokenSecret()
		{
			var sig = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				"base_string",
				"consumer_secret");
			Assert.False(string.IsNullOrEmpty(sig));
		}

		[Fact]
		public void OAuthTools_GetSignature_With_TokenSecret()
		{
			var sig = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				"base_string",
				"consumer_secret",
				"token_secret");
			Assert.False(string.IsNullOrEmpty(sig));
		}

		[Fact]
		public void OAuthTools_GetSignature_Unescaped()
		{
			var sig = OAuthTools.GetSignature(
				OAuthSignatureMethod.HmacSha1,
				OAuthSignatureTreatment.Unescaped,
				"base_string",
				"consumer_secret",
				"token_secret");
			Assert.False(string.IsNullOrEmpty(sig));
		}

		// WebPair tests
		[Fact]
		public void WebPair_Constructor_Sets_Properties()
		{
			var pair = new WebPair("key", "value");
			Assert.Equal("key", pair.Name);
			Assert.Equal("value", pair.Value);
		}

		[Fact]
		public void WebPair_Value_Can_Be_Changed()
		{
			var pair = new WebPair("key", "value");
			pair.Value = "newval";
			Assert.Equal("newval", pair.Value);
		}

		// WebParameter tests
		[Fact]
		public void WebParameter_Constructor()
		{
			var param = new WebParameter("name", "val");
			Assert.Equal("name", param.Name);
			Assert.Equal("val", param.Value);
		}

		// WebPairCollection tests
		[Fact]
		public void WebPairCollection_Add_By_Name_Value()
		{
			var coll = new WebPairCollection();
			coll.Add("key", "val");
			Assert.Single(coll);
		}

		[Fact]
		public void WebPairCollection_Indexer()
		{
			var coll = new WebPairCollection();
			coll.Add("key", "val");
			Assert.Equal("key", coll[0].Name);
		}

		[Fact]
		public void WebPairCollection_RemoveAll()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			coll.Add("c", "3");
			var toRemove = coll.Where(p => p.Name == "b");
			coll.RemoveAll(toRemove);
			Assert.Equal(2, coll.Count);
		}

		[Fact]
		public void WebPairCollection_Sort()
		{
			var coll = new WebPairCollection();
			coll.Add("z", "3");
			coll.Add("a", "1");
			coll.Add("m", "2");
			coll.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));
			Assert.Equal("a", coll[0].Name);
			Assert.Equal("m", coll[1].Name);
			Assert.Equal("z", coll[2].Name);
		}

		[Fact]
		public void WebPairCollection_AddCollection()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			var dict = new Dictionary<string, string> { { "b", "2" } };
			coll.AddCollection(dict);
			Assert.Equal(2, coll.Count);
		}

		// WebParameterCollection tests
		[Fact]
		public void WebParameterCollection_Add()
		{
			var coll = new WebParameterCollection();
			coll.Add("key", "val");
			Assert.Single(coll);
		}

		[Fact]
		public void WebParameterCollection_Constructor_With_Existing()
		{
			var existing = new WebParameterCollection();
			existing.Add("a", "1");
			existing.Add("b", "2");
			var copy = new WebParameterCollection(existing);
			Assert.Equal(2, copy.Count);
		}

		// OAuthExtensions tests
		[Fact]
		public void ToRequestValue_HmacSha1()
		{
			var result = OAuthSignatureMethod.HmacSha1.ToRequestValue();
			Assert.Equal("HMAC-SHA1", result);
		}

		[Fact]
		public void ToRequestValue_PlainText()
		{
			var result = OAuthSignatureMethod.PlainText.ToRequestValue();
			Assert.Equal("PLAINTEXT", result);
		}

		[Fact]
		public void ToRequestValue_RsaSha1()
		{
			var result = OAuthSignatureMethod.RsaSha1.ToRequestValue();
			Assert.Equal("RSA-SHA1", result);
		}

		[Fact]
		public void FromRequestValue_HmacSha1()
		{
			var result = "HMAC-SHA1".FromRequestValue();
			Assert.Equal(OAuthSignatureMethod.HmacSha1, result);
		}

		[Fact]
		public void FromRequestValue_RsaSha1()
		{
			var result = "RSA-SHA1".FromRequestValue();
			Assert.Equal(OAuthSignatureMethod.RsaSha1, result);
		}

		[Fact]
		public void FromRequestValue_PlainText()
		{
			var result = "PLAINTEXT".FromRequestValue();
			Assert.Equal(OAuthSignatureMethod.PlainText, result);
		}

		[Fact]
		public void HashWith_Produces_Result()
		{
			using (var hmac = new HMACSHA1(new byte[] { 1, 2, 3 }))
			{
				var result = "test".HashWith(hmac);
				Assert.False(string.IsNullOrEmpty(result));
			}
		}

		// TimeExtensions tests
		[Fact]
		public void ToUnixTime_Returns_Epoch_For_1970()
		{
			var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			Assert.Equal(0, dt.ToUnixTime());
		}

		[Fact]
		public void ToUnixTime_Returns_Positive_For_Recent()
		{
			var dt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			Assert.Equal(1577836800, dt.ToUnixTime());
		}

		[Fact]
		public void FromUnixTime_Returns_DateTime()
		{
			var dt = ((long)0).FromUnixTime();
			Assert.Equal(1970, dt.Year);
		}

		[Fact]
		public void FromNow_Returns_Future_DateTime()
		{
			var span = TimeSpan.FromHours(1);
			var result = span.FromNow();
			Assert.True(result > DateTime.Now.AddMinutes(59));
		}

		// OAuth StringExtensions tests
		[Fact]
		public void IsNullOrBlank_Null()
		{
			string s = null;
			Assert.True(s.IsNullOrBlank());
		}

		[Fact]
		public void IsNullOrBlank_Empty()
		{
			Assert.True("".IsNullOrBlank());
		}

		[Fact]
		public void IsNullOrBlank_Whitespace()
		{
			Assert.True("   ".IsNullOrBlank());
		}

		[Fact]
		public void IsNullOrBlank_NonEmpty()
		{
			Assert.False("hello".IsNullOrBlank());
		}

		[Fact]
		public void EqualsIgnoreCase_True()
		{
			Assert.True("Hello".EqualsIgnoreCase("hello"));
		}

		[Fact]
		public void EqualsIgnoreCase_False()
		{
			Assert.False("Hello".EqualsIgnoreCase("world"));
		}

		[Fact]
		public void EqualsAny_True()
		{
			Assert.True("hello".EqualsAny("world", "hello", "test"));
		}

		[Fact]
		public void EqualsAny_False()
		{
			Assert.False("hello".EqualsAny("world", "test"));
		}

		[Fact]
		public void FormatWith_Formats_String()
		{
			Assert.Equal("Hello World", "{0} {1}".FormatWith("Hello", "World"));
		}

		[Fact]
		public void FormatWithInvariantCulture_Formats()
		{
			Assert.Equal("Value: 42", "Value: {0}".FormatWithInvariantCulture(42));
		}

		[Fact]
		public void Then_Concatenates()
		{
			Assert.Equal("HelloWorld", "Hello".Then("World"));
		}

		[Fact]
		public void OAuthStringExt_UrlEncode()
		{
			var result = Authenticators.OAuth.Extensions.StringExtensions.UrlEncode("hello world");
			Assert.Equal("hello%20world", result);
		}

		[Fact]
		public void OAuthStringExt_UrlDecode()
		{
			var result = Authenticators.OAuth.Extensions.StringExtensions.UrlDecode("hello%20world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void AsUri_Converts_String()
		{
			var uri = "http://example.com".AsUri();
			Assert.Equal("http://example.com/", uri.ToString());
		}

		[Fact]
		public void ToBase64String_Converts()
		{
			var bytes = new byte[] { 72, 101, 108, 108, 111 };
			Assert.Equal("SGVsbG8=", bytes.ToBase64String());
		}

		[Fact]
		public void GetBytes_Converts()
		{
			var bytes = "Hi".GetBytes();
			Assert.Equal(2, bytes.Length);
		}

		[Fact]
		public void PercentEncode_Encodes()
		{
			var result = "A".PercentEncode();
			Assert.Equal("%41", result);
		}

		[Fact]
		public void ParseQueryString_Parses()
		{
			var result = "a=1&b=2".ParseQueryString();
			Assert.Equal("1", result["a"]);
			Assert.Equal("2", result["b"]);
		}

		[Fact]
		public void ParseQueryString_With_QuestionMark()
		{
			var result = "?a=1&b=2".ParseQueryString();
			Assert.Equal(2, result.Count);
		}

		[Fact]
		public void ParseQueryString_Empty()
		{
			var result = "".ParseQueryString();
			Assert.Empty(result);
		}

		// CollectionExtensions tests
		[Fact]
		public void AsEnumerable_Single_Item()
		{
			var result = "hello".AsEnumerable();
			Assert.Single(result);
		}

		[Fact]
		public void And_Two_Items()
		{
			var result = "hello".And("world");
			Assert.Equal(2, result.Count());
		}

		[Fact]
		public void And_Collection_Plus_Item()
		{
			var items = new List<string> { "a", "b" };
			var result = items.And("c").ToList();
			Assert.Equal(3, result.Count);
		}

		[Fact]
		public void TryWithKey_Existing_Key()
		{
			var dict = new Dictionary<string, int> { { "key", 42 } };
			Assert.Equal(42, dict.TryWithKey("key"));
		}

		[Fact]
		public void TryWithKey_Missing_Key()
		{
			var dict = new Dictionary<string, int> { { "key", 42 } };
			Assert.Equal(0, dict.TryWithKey("missing"));
		}

		[Fact]
		public void ForEach_Executes_Action()
		{
			var list = new List<int> { 1, 2, 3 };
			var sum = 0;
			list.ForEach(x => sum += x);
			Assert.Equal(6, sum);
		}

		[Fact]
		public void Concatenate_WebParameterCollection()
		{
			var coll = new WebParameterCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			var result = coll.Concatenate("=", "&");
			Assert.Equal("a=1&b=2", result);
		}

		[Fact]
		public void AddRange_From_NameValueCollection()
		{
			var dict = new Dictionary<string, string>();
			var nvc = new NameValueCollection();
			nvc.Add("key1", "val1");
			nvc.Add("key2", "val2");
			dict.AddRange(nvc);
			Assert.Equal(2, dict.Count);
		}

		[Fact]
		public void ToQueryString_From_NameValueCollection()
		{
			var nvc = new NameValueCollection();
			nvc.Add("a", "1");
			nvc.Add("b", "2");
			var result = nvc.ToQueryString();
			Assert.StartsWith("?", result);
			Assert.Contains("a=1", result);
		}

		[Fact]
		public void ToQueryString_Empty_Collection()
		{
			var nvc = new NameValueCollection();
			var result = nvc.ToQueryString();
			Assert.Equal("", result);
		}

		[Fact]
		public void ToEnumerable_Converts()
		{
			var objects = new object[] { "a", "b", "c" };
			var result = objects.ToEnumerable<string>().ToList();
			Assert.Equal(3, result.Count);
		}

		// OAuthWorkflow tests
		[Fact]
		public void OAuthWorkflow_Properties()
		{
			var wf = new OAuthWorkflow();
			wf.Version = "1.0";
			wf.ConsumerKey = "ck";
			wf.ConsumerSecret = "cs";
			wf.Token = "t";
			wf.TokenSecret = "ts";
			wf.CallbackUrl = "http://cb";
			wf.Verifier = "v";
			wf.SessionHandle = "sh";
			wf.SignatureMethod = OAuthSignatureMethod.HmacSha1;
			wf.SignatureTreatment = OAuthSignatureTreatment.Escaped;
			wf.ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader;
			wf.ClientUsername = "cu";
			wf.ClientPassword = "cp";
			wf.RequestTokenUrl = "http://rt";
			wf.AccessTokenUrl = "http://at";
			wf.AuthorizationUrl = "http://auth";

			Assert.Equal("1.0", wf.Version);
			Assert.Equal("ck", wf.ConsumerKey);
			Assert.Equal("cs", wf.ConsumerSecret);
			Assert.Equal("t", wf.Token);
			Assert.Equal("ts", wf.TokenSecret);
			Assert.Equal("http://cb", wf.CallbackUrl);
			Assert.Equal("v", wf.Verifier);
			Assert.Equal("sh", wf.SessionHandle);
			Assert.Equal("cu", wf.ClientUsername);
			Assert.Equal("cp", wf.ClientPassword);
			Assert.Equal("http://rt", wf.RequestTokenUrl);
			Assert.Equal("http://at", wf.AccessTokenUrl);
			Assert.Equal("http://auth", wf.AuthorizationUrl);
		}

		[Fact]
		public void OAuthWorkflow_BuildRequestTokenInfo()
		{
			var wf = new OAuthWorkflow
			{
				ConsumerKey = "key",
				ConsumerSecret = "secret",
				RequestTokenUrl = "http://example.com/request_token",
				SignatureMethod = OAuthSignatureMethod.HmacSha1,
				SignatureTreatment = OAuthSignatureTreatment.Escaped,
				ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
				Version = "1.0"
			};

			var info = wf.BuildRequestTokenInfo("GET");
			Assert.NotNull(info);
			Assert.Equal("GET", info.WebMethod);
			Assert.Equal("key", info.ConsumerKey);
			Assert.False(string.IsNullOrEmpty(info.Signature));
			Assert.False(string.IsNullOrEmpty(info.Nonce));
			Assert.False(string.IsNullOrEmpty(info.Timestamp));
		}

		[Fact]
		public void OAuthWorkflow_BuildAccessTokenInfo()
		{
			var wf = new OAuthWorkflow
			{
				ConsumerKey = "key",
				ConsumerSecret = "secret",
				Token = "request_token",
				TokenSecret = "request_secret",
				AccessTokenUrl = "http://example.com/access_token",
				SignatureMethod = OAuthSignatureMethod.HmacSha1,
				SignatureTreatment = OAuthSignatureTreatment.Escaped,
				ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
				Version = "1.0",
				Verifier = "verifier"
			};

			var info = wf.BuildAccessTokenInfo("POST");
			Assert.NotNull(info);
			Assert.Equal("POST", info.WebMethod);
		}

		[Fact]
		public void OAuthWorkflow_BuildProtectedResourceInfo()
		{
			var wf = new OAuthWorkflow
			{
				ConsumerKey = "key",
				ConsumerSecret = "secret",
				Token = "access_token",
				TokenSecret = "access_secret",
				SignatureMethod = OAuthSignatureMethod.HmacSha1,
				SignatureTreatment = OAuthSignatureTreatment.Escaped,
				ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
				Version = "1.0"
			};

			var info = wf.BuildProtectedResourceInfo("GET", null, "http://example.com/resource");
			Assert.NotNull(info);
			Assert.Equal("GET", info.WebMethod);
		}

		[Fact]
		public void OAuthWorkflow_BuildClientAuthAccessTokenInfo()
		{
			var wf = new OAuthWorkflow
			{
				ConsumerKey = "key",
				ConsumerSecret = "secret",
				AccessTokenUrl = "http://example.com/access_token",
				SignatureMethod = OAuthSignatureMethod.HmacSha1,
				SignatureTreatment = OAuthSignatureTreatment.Escaped,
				ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
				ClientUsername = "user",
				ClientPassword = "pass",
				Version = "1.0"
			};

			var info = wf.BuildClientAuthAccessTokenInfo("POST", null);
			Assert.NotNull(info);
			Assert.Equal("user", info.ClientUsername);
		}

		// HttpPostParameter tests
		[Fact]
		public void HttpPostParameter_Constructor()
		{
			var param = new HttpPostParameter("name", "value");
			Assert.Equal("name", param.Name);
			Assert.Equal("value", param.Value);
			Assert.Equal(HttpPostParameterType.Field, param.Type);
		}

		[Fact]
		public void HttpPostParameter_CreateFile_With_Path()
		{
			var param = HttpPostParameter.CreateFile("file", "test.txt", "/tmp/test.txt", "text/plain");
			Assert.Equal(HttpPostParameterType.File, param.Type);
			Assert.Equal("test.txt", param.FileName);
			Assert.Equal("text/plain", param.ContentType);
			Assert.Equal("/tmp/test.txt", param.FilePath);
		}

		[Fact]
		public void HttpPostParameter_CreateFile_With_Stream()
		{
			var stream = new System.IO.MemoryStream();
			var param = HttpPostParameter.CreateFile("file", "test.txt", stream, "text/plain");
			Assert.Equal(HttpPostParameterType.File, param.Type);
			Assert.Equal("test.txt", param.FileName);
			Assert.Same(stream, param.FileStream);
		}
	}
}
