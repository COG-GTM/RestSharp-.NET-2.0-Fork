using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using RestSharp.Authenticators.OAuth;
using Xunit;

namespace RestSharp.Tests
{
	public class WebPairCollectionTests
	{
		[Fact]
		public void Default_Constructor_Empty()
		{
			var coll = new WebPairCollection();
			Assert.Equal(0, coll.Count);
		}

		[Fact]
		public void Constructor_With_Capacity()
		{
			var coll = new WebPairCollection(10);
			Assert.Equal(0, coll.Count);
		}

		[Fact]
		public void Constructor_With_Pairs()
		{
			var pairs = new List<WebPair> { new WebPair("a", "1"), new WebPair("b", "2") };
			var coll = new WebPairCollection(pairs);
			Assert.Equal(2, coll.Count);
		}

		[Fact]
		public void Constructor_With_Dictionary()
		{
			var dict = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
			var coll = new WebPairCollection(dict);
			Assert.Equal(2, coll.Count);
		}

		[Fact]
		public void Constructor_With_NameValueCollection()
		{
			var nvc = new NameValueCollection { { "a", "1" }, { "b", "2" } };
			var coll = new WebPairCollection(nvc);
			Assert.Equal(2, coll.Count);
		}

		[Fact]
		public void Indexer_By_Name()
		{
			var coll = new WebPairCollection();
			coll.Add("test", "value");
			Assert.Equal("value", coll["test"].Value);
		}

		[Fact]
		public void Names_Property()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			Assert.Contains("a", coll.Names);
			Assert.Contains("b", coll.Names);
		}

		[Fact]
		public void Values_Property()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			Assert.Contains("1", coll.Values);
			Assert.Contains("2", coll.Values);
		}

		[Fact]
		public void AddRange_NameValueCollection()
		{
			var coll = new WebPairCollection();
			var nvc = new NameValueCollection { { "a", "1" } };
			coll.AddRange(nvc);
			Assert.Single(coll);
		}

		[Fact]
		public void AddRange_WebPairCollection()
		{
			var coll = new WebPairCollection();
			var other = new WebPairCollection();
			other.Add("a", "1");
			coll.AddRange(other);
			Assert.Single(coll);
		}

		[Fact]
		public void AddRange_IEnumerable()
		{
			var coll = new WebPairCollection();
			var list = new List<WebPair> { new WebPair("a", "1") };
			coll.AddRange(list);
			Assert.Single(coll);
		}

		[Fact]
		public void RemoveAll()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			var toRemove = coll.Where(p => p.Name == "a").ToList();
			var result = coll.RemoveAll(toRemove);
			Assert.True(result);
			Assert.Single(coll);
		}

		[Fact]
		public void RemoveAll_Empty_Returns_False()
		{
			var coll = new WebPairCollection();
			var result = coll.RemoveAll(new List<WebPair>());
			Assert.False(result);
		}

		[Fact]
		public void Clear()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Clear();
			Assert.Equal(0, coll.Count);
		}

		[Fact]
		public void Contains()
		{
			var coll = new WebPairCollection();
			var pair = new WebPair("a", "1");
			coll.Add(pair);
			Assert.True(coll.Contains(pair));
		}

		[Fact]
		public void CopyTo()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			var array = new WebPair[2];
			coll.CopyTo(array, 0);
			Assert.Equal("a", array[0].Name);
		}

		[Fact]
		public void Remove()
		{
			var coll = new WebPairCollection();
			var pair = new WebPair("a", "1");
			coll.Add(pair);
			Assert.True(coll.Remove(pair));
			Assert.Equal(0, coll.Count);
		}

		[Fact]
		public void IsReadOnly()
		{
			var coll = new WebPairCollection();
			Assert.False(coll.IsReadOnly);
		}

		[Fact]
		public void IndexOf()
		{
			var coll = new WebPairCollection();
			var pair = new WebPair("a", "1");
			coll.Add(pair);
			Assert.Equal(0, coll.IndexOf(pair));
		}

		[Fact]
		public void Insert()
		{
			var coll = new WebPairCollection();
			coll.Add("b", "2");
			coll.Insert(0, new WebPair("a", "1"));
			Assert.Equal("a", coll[0].Name);
		}

		[Fact]
		public void RemoveAt()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			coll.RemoveAt(0);
			Assert.Single(coll);
			Assert.Equal("b", coll[0].Name);
		}

		[Fact]
		public void IntIndexer_Set()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll[0] = new WebPair("b", "2");
			Assert.Equal("b", coll[0].Name);
		}

		[Fact]
		public void Sort()
		{
			var coll = new WebPairCollection();
			coll.Add("c", "3");
			coll.Add("a", "1");
			coll.Add("b", "2");
			coll.Sort((l, r) => string.Compare(l.Name, r.Name, StringComparison.Ordinal));
			Assert.Equal("a", coll[0].Name);
			Assert.Equal("b", coll[1].Name);
			Assert.Equal("c", coll[2].Name);
		}

		[Fact]
		public void GetEnumerator_Enumerates()
		{
			var coll = new WebPairCollection();
			coll.Add("a", "1");
			coll.Add("b", "2");
			var count = 0;
			foreach (var item in coll)
			{
				count++;
			}
			Assert.Equal(2, count);
		}

		[Fact]
		public void Add_WebPair_Object()
		{
			var coll = new WebPairCollection();
			coll.Add(new WebPair("key", "val"));
			Assert.Equal("key", coll[0].Name);
		}
	}
}
