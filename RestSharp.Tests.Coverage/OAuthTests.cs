using System;
using System.Linq;
using RestSharp.Authenticators.OAuth;
using Xunit;

namespace RestSharp.Tests
{
	public class OAuthTests
	{
		[Fact]
		public void OAuthSignatureMethod_Enum_Values()
		{
			Assert.Equal(OAuthSignatureMethod.HmacSha1, (OAuthSignatureMethod)0);
			Assert.Equal(OAuthSignatureMethod.PlainText, (OAuthSignatureMethod)1);
			Assert.Equal(OAuthSignatureMethod.RsaSha1, (OAuthSignatureMethod)2);
		}

		[Fact]
		public void OAuthParameterHandling_Enum_Values()
		{
			Assert.Equal(OAuthParameterHandling.HttpAuthorizationHeader, (OAuthParameterHandling)0);
			Assert.Equal(OAuthParameterHandling.UrlOrPostParameters, (OAuthParameterHandling)1);
		}

		[Fact]
		public void OAuthSignatureTreatment_Enum_Values()
		{
			Assert.Equal(OAuthSignatureTreatment.Escaped, (OAuthSignatureTreatment)0);
			Assert.Equal(OAuthSignatureTreatment.Unescaped, (OAuthSignatureTreatment)1);
		}

		[Fact]
		public void OAuthType_Enum_Values()
		{
			Assert.Equal(OAuthType.RequestToken, (OAuthType)0);
			Assert.Equal(OAuthType.AccessToken, (OAuthType)1);
			Assert.Equal(OAuthType.ProtectedResource, (OAuthType)2);
			Assert.Equal(OAuthType.ClientAuthentication, (OAuthType)3);
		}

		[Fact]
		public void OAuthWebQueryInfo_ConsumerKey_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.ConsumerKey = "ck";
			Assert.Equal("ck", info.ConsumerKey);
		}

		[Fact]
		public void OAuthWebQueryInfo_Token_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Token = "t";
			Assert.Equal("t", info.Token);
		}

		[Fact]
		public void OAuthWebQueryInfo_Nonce_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Nonce = "n";
			Assert.Equal("n", info.Nonce);
		}

		[Fact]
		public void OAuthWebQueryInfo_Timestamp_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Timestamp = "123";
			Assert.Equal("123", info.Timestamp);
		}

		[Fact]
		public void OAuthWebQueryInfo_Signature_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Signature = "sig";
			Assert.Equal("sig", info.Signature);
		}

		[Fact]
		public void OAuthWebQueryInfo_SignatureMethod_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.SignatureMethod = "HMAC-SHA1";
			Assert.Equal("HMAC-SHA1", info.SignatureMethod);
		}

		[Fact]
		public void OAuthWebQueryInfo_Version_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Version = "1.0";
			Assert.Equal("1.0", info.Version);
		}

		[Fact]
		public void OAuthWebQueryInfo_Callback_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Callback = "http://cb";
			Assert.Equal("http://cb", info.Callback);
		}

		[Fact]
		public void OAuthWebQueryInfo_Verifier_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.Verifier = "v";
			Assert.Equal("v", info.Verifier);
		}

		[Fact]
		public void OAuthWebQueryInfo_WebMethod_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.WebMethod = "POST";
			Assert.Equal("POST", info.WebMethod);
		}

		[Fact]
		public void OAuthWebQueryInfo_ParameterHandling_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader;
			Assert.Equal(OAuthParameterHandling.HttpAuthorizationHeader, info.ParameterHandling);
		}

		[Fact]
		public void OAuthWebQueryInfo_ClientMode_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.ClientMode = "client";
			Assert.Equal("client", info.ClientMode);
		}

		[Fact]
		public void OAuthWebQueryInfo_ClientUsername_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.ClientUsername = "cu";
			Assert.Equal("cu", info.ClientUsername);
		}

		[Fact]
		public void OAuthWebQueryInfo_ClientPassword_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.ClientPassword = "cp";
			Assert.Equal("cp", info.ClientPassword);
		}

		[Fact]
		public void OAuthWebQueryInfo_UserAgent_Property()
		{
			var info = new OAuthWebQueryInfo();
			info.UserAgent = "ua";
			Assert.Equal("ua", info.UserAgent);
		}
	}
}
