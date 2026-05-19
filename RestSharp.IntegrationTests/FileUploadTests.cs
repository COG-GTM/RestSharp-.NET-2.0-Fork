using System;
using System.IO;
using System.Text;
using RestSharp.IntegrationTests.Helpers;
using Xunit;

namespace RestSharp.IntegrationTests
{
	public class FileUploadTests
	{
		private const string BaseUrl = "http://localhost:8891/";

		[Fact]
		public void Upload_Single_File()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				var bytes = Encoding.UTF8.GetBytes("file content here");
				request.AddFile("file", bytes, "test.txt", "text/plain");

				var response = client.Execute(request);
				Assert.Contains("test.txt", response.Content);
				Assert.Contains("Content-Disposition", response.Content);
			}
		}

		[Fact]
		public void Upload_Multiple_Files()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddFile("file1", Encoding.UTF8.GetBytes("content1"), "test1.txt", "text/plain");
				request.AddFile("file2", Encoding.UTF8.GetBytes("content2"), "test2.txt", "text/plain");

				var response = client.Execute(request);
				Assert.Contains("test1.txt", response.Content);
				Assert.Contains("test2.txt", response.Content);
			}
		}

		[Fact]
		public void Upload_File_With_Parameters()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddFile("file", Encoding.UTF8.GetBytes("content"), "test.txt", "text/plain");
				request.AddParameter("key", "value");

				var response = client.Execute(request);
				Assert.Contains("test.txt", response.Content);
				Assert.Contains("key", response.Content);
			}
		}

		[Fact]
		public void Upload_File_With_Custom_ContentType()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddFile("file", Encoding.UTF8.GetBytes("content"), "image.png", "image/png");

				var response = client.Execute(request);
				Assert.Contains("image/png", response.Content);
			}
		}

		[Fact]
		public void Upload_File_With_Byte_Array()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				request.AddFile("file", new byte[] { 0x01, 0x02, 0x03 }, "binary.bin");

				var response = client.Execute(request);
				Assert.Contains("binary.bin", response.Content);
			}
		}

		[Fact]
		public void Upload_File_With_Writer()
		{
			using (SimpleServer.Create(BaseUrl, Handlers.Echo))
			{
				var client = new RestClient(BaseUrl);
				var request = new RestRequest("", Method.POST);
				Action<Stream> writer = s =>
				{
					var data = Encoding.UTF8.GetBytes("writer content");
					s.Write(data, 0, data.Length);
				};
				request.AddFile("file", writer, "writer.txt", "text/plain");

				var response = client.Execute(request);
				Assert.Contains("writer.txt", response.Content);
			}
		}
	}
}
