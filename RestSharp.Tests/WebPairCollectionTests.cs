using System.Collections.Generic;
using System.Linq;
using RestSharp.Authenticators.OAuth;
using Xunit;

namespace RestSharp.Tests
{
	public class WebPairCollectionTests
	{
		// WebPairCollection tests
		[Fact]
		public void Add_String_Pair()
		{
			var collection = new WebPairCollection();
			collection.Add("key", "value");
			Assert.Equal(1, collection.Count);
			Assert.Equal("key", collection[0].Name);
			Assert.Equal("value", collection[0].Value);
		}

		[Fact]
		public void Add_WebPair()
		{
			var collection = new WebPairCollection();
			collection.Add(new WebPair("key", "value"));
			Assert.Equal(1, collection.Count);
		}

		[Fact]
		public void Remove_WebPair()
		{
			var pair = new WebPair("key", "value");
			var collection = new WebPairCollection();
			collection.Add(pair);
			Assert.Equal(1, collection.Count);

			collection.Remove(pair);
			Assert.Equal(0, collection.Count);
		}

		[Fact]
		public void Indexer_By_Name()
		{
			var collection = new WebPairCollection();
			collection.Add("key", "value");
			var result = collection["key"];
			Assert.NotNull(result);
			Assert.Equal("value", result.Value);
		}

		[Fact]
		public void Indexer_By_Int()
		{
			var collection = new WebPairCollection();
			collection.Add("key1", "value1");
			collection.Add("key2", "value2");
			Assert.Equal("key1", collection[0].Name);
			Assert.Equal("key2", collection[1].Name);
		}

		[Fact]
		public void Clear_Removes_All()
		{
			var collection = new WebPairCollection();
			collection.Add("key1", "value1");
			collection.Add("key2", "value2");
			collection.Clear();
			Assert.Equal(0, collection.Count);
		}

		[Fact]
		public void Names_Returns_All_Names()
		{
			var collection = new WebPairCollection();
			collection.Add("key1", "value1");
			collection.Add("key2", "value2");
			var names = collection.Names.ToList();
			Assert.Contains("key1", names);
			Assert.Contains("key2", names);
		}

		[Fact]
		public void Values_Returns_All_Values()
		{
			var collection = new WebPairCollection();
			collection.Add("key1", "value1");
			collection.Add("key2", "value2");
			var values = collection.Values.ToList();
			Assert.Contains("value1", values);
			Assert.Contains("value2", values);
		}

		[Fact]
		public void Sort_Orders_By_Comparison()
		{
			var collection = new WebPairCollection();
			collection.Add("z", "3");
			collection.Add("a", "1");
			collection.Add("m", "2");

			collection.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));

			Assert.Equal("a", collection[0].Name);
			Assert.Equal("m", collection[1].Name);
			Assert.Equal("z", collection[2].Name);
		}

		[Fact]
		public void RemoveAll_Removes_Specified()
		{
			var collection = new WebPairCollection();
			var pair1 = new WebPair("key1", "value1");
			var pair2 = new WebPair("key2", "value2");
			collection.Add(pair1);
			collection.Add(pair2);

			collection.RemoveAll(new[] { pair1 });
			Assert.Equal(1, collection.Count);
			Assert.Equal("key2", collection[0].Name);
		}

		[Fact]
		public void Contains_Returns_True_For_Existing()
		{
			var pair = new WebPair("key", "value");
			var collection = new WebPairCollection();
			collection.Add(pair);
			Assert.True(collection.Contains(pair));
		}

		[Fact]
		public void AddRange_From_Dictionary()
		{
			var dict = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
			var collection = new WebPairCollection(dict);
			Assert.Equal(2, collection.Count);
		}

		[Fact]
		public void Constructor_From_IEnumerable()
		{
			var pairs = new List<WebPair> { new WebPair("a", "1"), new WebPair("b", "2") };
			var collection = new WebPairCollection(pairs);
			Assert.Equal(2, collection.Count);
		}

		// WebParameterCollection tests
		[Fact]
		public void WebParameterCollection_Add_Creates_WebParameter()
		{
			var collection = new WebParameterCollection();
			collection.Add("key", "value");
			Assert.Equal(1, collection.Count);
			Assert.IsType<WebParameter>(collection[0]);
		}

		[Fact]
		public void WebParameterCollection_Constructor_From_Dictionary()
		{
			var dict = new Dictionary<string, string> { { "key1", "value1" } };
			var collection = new WebParameterCollection(dict);
			Assert.Equal(1, collection.Count);
		}

		[Fact]
		public void WebParameterCollection_Constructor_From_IEnumerable()
		{
			var pairs = new List<WebPair> { new WebPair("a", "1"), new WebPair("b", "2") };
			var collection = new WebParameterCollection(pairs);
			Assert.Equal(2, collection.Count);
		}
	}
}
