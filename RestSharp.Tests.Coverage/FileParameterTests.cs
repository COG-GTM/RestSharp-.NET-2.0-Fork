using System.IO;
using Xunit;

namespace RestSharp.Tests
{
	public class FileParameterTests
	{
		[Fact]
		public void Create_With_Name_Data_Filename_ContentType()
		{
			var data = new byte[] { 1, 2, 3, 4, 5 };
			var fp = FileParameter.Create("file", data, "test.txt", "text/plain");

			Assert.Equal("file", fp.Name);
			Assert.Equal("test.txt", fp.FileName);
			Assert.Equal("text/plain", fp.ContentType);
			Assert.Equal(5, fp.ContentLength);
			Assert.NotNull(fp.Writer);
		}

		[Fact]
		public void Create_With_Name_Data_Filename_No_ContentType()
		{
			var data = new byte[] { 10, 20, 30 };
			var fp = FileParameter.Create("doc", data, "doc.pdf");

			Assert.Equal("doc", fp.Name);
			Assert.Equal("doc.pdf", fp.FileName);
			Assert.Null(fp.ContentType);
			Assert.Equal(3, fp.ContentLength);
		}

		[Fact]
		public void Writer_Writes_Data_To_Stream()
		{
			var data = new byte[] { 1, 2, 3, 4, 5 };
			var fp = FileParameter.Create("file", data, "test.bin", "application/octet-stream");

			using (var ms = new MemoryStream())
			{
				fp.Writer(ms);
				Assert.Equal(data, ms.ToArray());
			}
		}

		[Fact]
		public void Properties_Can_Be_Set()
		{
			var fp = new FileParameter();
			fp.Name = "myfile";
			fp.FileName = "upload.jpg";
			fp.ContentType = "image/jpeg";
			fp.ContentLength = 12345;
			fp.Writer = s => { };

			Assert.Equal("myfile", fp.Name);
			Assert.Equal("upload.jpg", fp.FileName);
			Assert.Equal("image/jpeg", fp.ContentType);
			Assert.Equal(12345, fp.ContentLength);
			Assert.NotNull(fp.Writer);
		}

		[Fact]
		public void Create_Empty_Data()
		{
			var data = new byte[0];
			var fp = FileParameter.Create("empty", data, "empty.txt", "text/plain");

			Assert.Equal(0, fp.ContentLength);
			using (var ms = new MemoryStream())
			{
				fp.Writer(ms);
				Assert.Empty(ms.ToArray());
			}
		}
	}
}
