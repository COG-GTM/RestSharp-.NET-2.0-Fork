#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using RestSharp.Tests.SampleClasses;
using Xunit;

namespace RestSharp.Tests
{
	public class ErrorHandlingTests
	{
		[Fact]
		public void JsonDeserializer_Handles_Malformed_JSON_With_Unclosed_Bracket()
		{
			const string malformedJson = "{\"Name\": \"John\", \"Age\": 30";
			var deserializer = new JsonDeserializer();
			var response = new RestResponse { Content = malformedJson };

			Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
			{
				deserializer.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Malformed_JSON_With_Invalid_Syntax()
		{
			const string malformedJson = "{Name: John, Age: 30}";
			var deserializer = new JsonDeserializer();
			var response = new RestResponse { Content = malformedJson };

			Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
			{
				deserializer.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Malformed_JSON_With_Trailing_Comma()
		{
			const string malformedJson = "{\"Name\": \"John\", \"Age\": 30,}";
			var deserializer = new JsonDeserializer();
			var response = new RestResponse { Content = malformedJson };

			Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
			{
				deserializer.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Malformed_JSON_Array_With_Unclosed_Bracket()
		{
			const string malformedJson = "[{\"Name\": \"John\"}, {\"Name\": \"Jane\"";
			var deserializer = new JsonDeserializer();
			var response = new RestResponse { Content = malformedJson };

			Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
			{
				deserializer.Deserialize<List<PersonForJson>>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Empty_String()
		{
			const string emptyJson = "";
			var deserializer = new JsonDeserializer();
			var response = new RestResponse { Content = emptyJson };

			Assert.Throws<System.ArgumentException>(() =>
			{
				deserializer.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Malformed_XML_With_Unclosed_Tag()
		{
			const string malformedXml = "<Person><Name>John</Name><Age>30</Person>";
			var deserializer = new XmlDeserializer();
			var response = new RestResponse { Content = malformedXml };

			Assert.Throws<System.Xml.XmlException>(() =>
			{
				deserializer.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Malformed_XML_With_Invalid_Tag_Name()
		{
			const string malformedXml = "<Person><Name>John</Name><123Age>30</123Age></Person>";
			var deserializer = new XmlDeserializer();
			var response = new RestResponse { Content = malformedXml };

			Assert.Throws<System.Xml.XmlException>(() =>
			{
				deserializer.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Malformed_XML_With_Mismatched_Tags()
		{
			const string malformedXml = "<Person><Name>John</Age></Person>";
			var deserializer = new XmlDeserializer();
			var response = new RestResponse { Content = malformedXml };

			Assert.Throws<System.Xml.XmlException>(() =>
			{
				deserializer.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Malformed_XML_With_Unclosed_Root()
		{
			const string malformedXml = "<Person><Name>John</Name><Age>30</Age>";
			var deserializer = new XmlDeserializer();
			var response = new RestResponse { Content = malformedXml };

			Assert.Throws<System.Xml.XmlException>(() =>
			{
				deserializer.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Empty_String()
		{
			const string emptyXml = "";
			var deserializer = new XmlDeserializer();
			var response = new RestResponse { Content = emptyXml };

			var result = deserializer.Deserialize<PersonForXml>(response);

			Assert.Null(result);
		}

		[Fact]
		public void JsonDeserializer_Handles_Invalid_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			const string invalidFormat = "INVALID_FORMAT";
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var formatted = new
			{
				StartDate = date.ToString("yyyy-MM-dd", culture)
			};

			var data = Newtonsoft.Json.JsonConvert.SerializeObject(formatted);
			var response = new RestResponse { Content = data };

			var json = new JsonDeserializer { DateFormat = invalidFormat, Culture = culture };

			Assert.Throws<System.FormatException>(() =>
			{
				json.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Date_That_Does_Not_Match_Format()
		{
			var culture = CultureInfo.InvariantCulture;
			const string format = "yyyy-MM-dd";

			var formatted = new
			{
				StartDate = "2010/02/08"
			};

			var data = Newtonsoft.Json.JsonConvert.SerializeObject(formatted);
			var response = new RestResponse { Content = data };

			var json = new JsonDeserializer { DateFormat = format, Culture = culture };

			Assert.Throws<System.FormatException>(() =>
			{
				json.Deserialize<PersonForJson>(response);
			});
		}

		[Fact]
		public void JsonDeserializer_Handles_Null_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var formatted = new
			{
				StartDate = date.ToString("yyyy-MM-dd", culture)
			};

			var data = Newtonsoft.Json.JsonConvert.SerializeObject(formatted);
			var response = new RestResponse { Content = data };

			var json = new JsonDeserializer { DateFormat = null, Culture = culture };

			var output = json.Deserialize<PersonForJson>(response);

			Assert.NotNull(output);
			Assert.Equal(date.Date, output.StartDate.Date);
		}

		[Fact]
		public void JsonDeserializer_Handles_Empty_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var formatted = new
			{
				StartDate = date.ToString("yyyy-MM-dd", culture)
			};

			var data = Newtonsoft.Json.JsonConvert.SerializeObject(formatted);
			var response = new RestResponse { Content = data };

			var json = new JsonDeserializer { DateFormat = "", Culture = culture };

			var output = json.Deserialize<PersonForJson>(response);

			Assert.NotNull(output);
			Assert.Equal(date.Date, output.StartDate.Date);
		}

		[Fact]
		public void XmlDeserializer_Handles_Invalid_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			const string invalidFormat = "INVALID_FORMAT";
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var doc = new System.Xml.Linq.XDocument();
			var root = new System.Xml.Linq.XElement("Person");
			root.Add(new System.Xml.Linq.XElement("StartDate", date.ToString("yyyy-MM-dd", culture)));
			doc.Add(root);

			var xml = new XmlDeserializer
			{
				DateFormat = invalidFormat,
				Culture = culture
			};

			var response = new RestResponse { Content = doc.ToString() };

			Assert.Throws<System.FormatException>(() =>
			{
				xml.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Date_That_Does_Not_Match_Format()
		{
			var culture = CultureInfo.InvariantCulture;
			const string format = "yyyy-MM-dd";
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var doc = new System.Xml.Linq.XDocument();
			var root = new System.Xml.Linq.XElement("Person");
			root.Add(new System.Xml.Linq.XElement("StartDate", date.ToString("MM/dd/yyyy", culture)));
			doc.Add(root);

			var xml = new XmlDeserializer
			{
				DateFormat = format,
				Culture = culture
			};

			var response = new RestResponse { Content = doc.ToString() };

			Assert.Throws<System.FormatException>(() =>
			{
				xml.Deserialize<PersonForXml>(response);
			});
		}

		[Fact]
		public void XmlDeserializer_Handles_Null_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var doc = new System.Xml.Linq.XDocument();
			var root = new System.Xml.Linq.XElement("Person");
			root.Add(new System.Xml.Linq.XElement("StartDate", date.ToString(culture)));
			doc.Add(root);

			var xml = new XmlDeserializer
			{
				DateFormat = null,
				Culture = culture
			};

			var response = new RestResponse { Content = doc.ToString() };
			var output = xml.Deserialize<PersonForXml>(response);

			Assert.NotNull(output);
			Assert.Equal(date, output.StartDate);
		}

		[Fact]
		public void XmlDeserializer_Handles_Empty_Date_Format_String()
		{
			var culture = CultureInfo.InvariantCulture;
			var date = new DateTime(2010, 2, 8, 11, 11, 11);

			var doc = new System.Xml.Linq.XDocument();
			var root = new System.Xml.Linq.XElement("Person");
			root.Add(new System.Xml.Linq.XElement("StartDate", date.ToString(culture)));
			doc.Add(root);

			var xml = new XmlDeserializer
			{
				DateFormat = "",
				Culture = culture
			};

			var response = new RestResponse { Content = doc.ToString() };
			var output = xml.Deserialize<PersonForXml>(response);

			Assert.NotNull(output);
			Assert.Equal(date, output.StartDate);
		}

		[Fact]
		public void XmlSerializer_Handles_Circular_Reference_In_Object_Graph()
		{
			var parent = new CircularReferenceParent { Name = "Parent" };
			var child = new CircularReferenceChild { Name = "Child", Parent = parent };
			parent.Child = child;

			var serializer = new XmlSerializer();

			var result = serializer.Serialize(parent);

			Assert.NotNull(result);
			Assert.Contains("<Name>Parent</Name>", result);
			Assert.Contains("<Name>Child</Name>", result);
		}

		[Fact]
		public void XmlSerializer_Handles_Self_Referencing_Object()
		{
			var node = new SelfReferencingNode { Name = "Node1" };
			node.Next = node;

			var serializer = new XmlSerializer();

			var result = serializer.Serialize(node);

			Assert.NotNull(result);
			Assert.Contains("<Name>Node1</Name>", result);
		}

		[Fact]
		public void XmlSerializer_Handles_Circular_Reference_In_List()
		{
			var parent = new CircularReferenceParent { Name = "Parent" };
			var child1 = new CircularReferenceChild { Name = "Child1", Parent = parent };
			var child2 = new CircularReferenceChild { Name = "Child2", Parent = parent };
			
			parent.Children = new List<CircularReferenceChild> { child1, child2 };

			var serializer = new XmlSerializer();

			var result = serializer.Serialize(parent);

			Assert.NotNull(result);
			Assert.Contains("<Name>Parent</Name>", result);
		}

		public class CircularReferenceParent
		{
			public string Name { get; set; }
			public CircularReferenceChild Child { get; set; }
			public List<CircularReferenceChild> Children { get; set; }
		}

		public class CircularReferenceChild
		{
			public string Name { get; set; }
			public CircularReferenceParent Parent { get; set; }
		}

		public class SelfReferencingNode
		{
			public string Name { get; set; }
			public SelfReferencingNode Next { get; set; }
		}
	}
}
