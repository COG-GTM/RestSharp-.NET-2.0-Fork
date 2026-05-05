using System;
using Xunit;

namespace RestSharp.Tests
{
	public class FactoryAndEnumTests
	{
		[Fact]
		public void SimpleFactory_Creates_Instance()
		{
			var factory = new SimpleFactory<Http>();
			var http = factory.Create();
			Assert.NotNull(http);
			Assert.IsType<Http>(http);
		}

		[Fact]
		public void Method_Enum_Values()
		{
			Assert.Equal(Method.GET, (Method)0);
			Assert.Equal(Method.POST, (Method)1);
			Assert.Equal(Method.PUT, (Method)2);
			Assert.Equal(Method.DELETE, (Method)3);
			Assert.Equal(Method.HEAD, (Method)4);
			Assert.Equal(Method.OPTIONS, (Method)5);
			Assert.Equal(Method.PATCH, (Method)6);
		}

		[Fact]
		public void DataFormat_Enum_Values()
		{
			Assert.Equal(DataFormat.Json, (DataFormat)0);
			Assert.Equal(DataFormat.Xml, (DataFormat)1);
		}

		[Fact]
		public void ResponseStatus_Enum_Values()
		{
			Assert.Equal(ResponseStatus.None, (ResponseStatus)0);
			Assert.Equal(ResponseStatus.Completed, (ResponseStatus)1);
			Assert.Equal(ResponseStatus.Error, (ResponseStatus)2);
			Assert.Equal(ResponseStatus.TimedOut, (ResponseStatus)3);
			Assert.Equal(ResponseStatus.Aborted, (ResponseStatus)4);
		}

		[Fact]
		public void DateFormat_Constants()
		{
			Assert.Equal("s", DateFormat.Iso8601);
			Assert.Equal("u", DateFormat.RoundTrip);
		}

		[Fact]
		public void RestRequestAsyncHandle_Defaults()
		{
			var handle = new RestRequestAsyncHandle();
			Assert.Null(handle.WebRequest);
		}

		[Fact]
		public void RestRequestAsyncHandle_Abort_Without_WebRequest()
		{
			var handle = new RestRequestAsyncHandle();
			// should not throw
			handle.Abort();
		}
	}
}
