using System.Globalization;
using System.Xml.Linq;
using NUnit.Framework;
using RestSharp.Serializers.Xml;
using RestSharp.Tests.SampleClasses;

namespace RestSharp.Tests;

[TestFixture]
public class XmlTests
{
    private const string GuidString = "AC1FC4BC-087A-4242-B8EE-C53EBE9887A5";
    private readonly string _sampleDataPath = Path.Combine(AppContext.BaseDirectory, "SampleData");

    private string PathFor(string sampleFile) => Path.Combine(_sampleDataPath, sampleFile);

    [Test]
    public void Can_Deserialize_Lists_of_Simple_Types()
    {
        var xmlpath = PathFor("xmllists.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<SimpleTypesListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Names, Is.Not.Empty);
        Assert.That(output.Numbers, Is.Not.Empty);
        Assert.That(output.Names![0].Length, Is.Not.EqualTo(0));
        Assert.That(output.Numbers!.Sum(), Is.Not.EqualTo(0));
    }

    [Test]
    public void Can_Deserialize_To_List_Inheritor_From_Custom_Root_With_Attributes()
    {
        var xmlpath = PathFor("ListWithAttributes.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer { RootElement = "Calls" };
        var output = xml.Deserialize<TwilioCallList>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.NumPages, Is.EqualTo(3));
        Assert.That(output, Is.Not.Empty);
        Assert.That(output.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Deserialize_To_Standalone_List_Without_Matching_Class_Case()
    {
        var xmlpath = PathFor("InlineListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<List<Image>>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Empty);
        Assert.That(output!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_To_Standalone_List_With_Matching_Class_Case()
    {
        var xmlpath = PathFor("InlineListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<List<image>>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Empty);
        Assert.That(output!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Directly_To_Lists_Off_Root_Element()
    {
        var xmlpath = PathFor("directlists.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<List<Database>>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Empty);
        Assert.That(output!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Deserialize_Parentless_aka_Inline_List_Items_Without_Matching_Class_Name()
    {
        var xmlpath = PathFor("InlineListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<InlineListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Images, Is.Not.Empty);
        Assert.That(output.Images!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Parentless_aka_Inline_List_Items_With_Matching_Class_Name()
    {
        var xmlpath = PathFor("InlineListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<InlineListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.images, Is.Not.Empty);
        Assert.That(output.images!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Parentless_aka_Inline_List_Items_With_Matching_Class_Name_With_Additional_Property()
    {
        var xmlpath = PathFor("InlineListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<InlineListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Nested_List_Items_Without_Matching_Class_Name()
    {
        var xmlpath = PathFor("NestedListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<InlineListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Images, Is.Not.Empty);
        Assert.That(output.Images!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Nested_List_Items_With_Matching_Class_Name()
    {
        var xmlpath = PathFor("NestedListSample.xml");
        var doc = XDocument.Load(xmlpath);

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<InlineListSample>(new RestResponse { Content = doc.ToString() });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.images, Is.Not.Empty);
        Assert.That(output.images!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Nested_List_Without_Elements_To_Empty_List()
    {
        var doc = CreateXmlWithEmptyNestedList();

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<EmptyListSample>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.images, Is.Not.Null);
        Assert.That(output.Images, Is.Not.Null);
        Assert.That(output.images, Is.Empty);
        Assert.That(output.Images, Is.Empty);
    }

    [Test]
    public void Can_Deserialize_Inline_List_Without_Elements_To_Empty_List()
    {
        var doc = CreateXmlWithEmptyInlineList();

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<EmptyListSample>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.images, Is.Not.Null);
        Assert.That(output.Images, Is.Not.Null);
        Assert.That(output.images, Is.Empty);
        Assert.That(output.Images, Is.Empty);
    }

    [Test]
    public void Can_Deserialize_Empty_Elements_to_Nullable_Values()
    {
        var doc = CreateXmlWithNullValues();

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<NullableValues>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Null);
        Assert.That(output.StartDate, Is.Null);
        Assert.That(output.UniqueId, Is.Null);
    }

    [Test]
    public void Can_Deserialize_Elements_to_Nullable_Values()
    {
        var culture = CultureInfo.InvariantCulture;
        var doc = CreateXmlWithoutEmptyValues(culture);
        var xml = new XmlDeserializer { Culture = culture };
        var output = xml.Deserialize<NullableValues>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Not.Null);
        Assert.That(output.StartDate, Is.Not.Null);
        Assert.That(output.UniqueId, Is.Not.Null);

        Assert.That(output.Id, Is.EqualTo(123));
        Assert.That(output.StartDate, Is.EqualTo(new DateTime(2010, 2, 21, 9, 35, 00)));
        Assert.That(output.UniqueId, Is.EqualTo(new Guid(GuidString)));
    }

    [Test]
    public void Can_Deserialize_Custom_Formatted_Date()
    {
        var culture = CultureInfo.InvariantCulture;
        var format = "dd yyyy MMM, hh:mm ss tt zzz";
        var date = new DateTime(2010, 2, 8, 11, 11, 11);

        var doc = new XDocument();
        var root = new XElement("Person");
        root.Add(new XElement("StartDate", date.ToString(format, culture)));
        doc.Add(root);

        var xml = new XmlDeserializer
        {
            DateFormat = format,
            Culture = culture
        };

        var response = new RestResponse { Content = doc.ToString() };
        var output = xml.Deserialize<PersonForXml>(response);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.StartDate, Is.EqualTo(date));
    }

    [Test]
    public void Can_Deserialize_Elements_On_Default_Root()
    {
        var doc = CreateElementsXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Name, Is.EqualTo("John Sheehan"));
        Assert.That(p.StartDate, Is.EqualTo(new DateTime(2009, 9, 25, 0, 6, 1)));
        Assert.That(p.Age, Is.EqualTo(28));
        Assert.That(p.BigNumber, Is.EqualTo(long.MaxValue));
        Assert.That(p.Percent, Is.EqualTo(99.9999m));
        Assert.That(p.IsCool, Is.False);
        Assert.That(p.UniqueId, Is.EqualTo(new Guid(GuidString)));
        Assert.That(p.EmptyGuid, Is.EqualTo(Guid.Empty));
        Assert.That(p.Url, Is.EqualTo(new Uri("http://example.com", UriKind.RelativeOrAbsolute)));
        Assert.That(p.UrlPath, Is.EqualTo(new Uri("/foo/bar", UriKind.RelativeOrAbsolute)));
        Assert.That(p.Order, Is.EqualTo(Order.Third));
        Assert.That(p.Disposition, Is.EqualTo(Disposition.SoSo));

        Assert.That(p.Friends, Is.Not.Null);
        Assert.That(p.Friends!.Count, Is.EqualTo(10));

        Assert.That(p.BestFriend, Is.Not.Null);
        Assert.That(p.BestFriend!.Name, Is.EqualTo("The Fonz"));
        Assert.That(p.BestFriend.Since, Is.EqualTo(1952));
    }

    [Test]
    public void Can_Deserialize_Attributes_On_Default_Root()
    {
        var doc = CreateAttributesXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Name, Is.EqualTo("John Sheehan"));
        Assert.That(p.StartDate, Is.EqualTo(new DateTime(2009, 9, 25, 0, 6, 1)));
        Assert.That(p.Age, Is.EqualTo(28));
        Assert.That(p.BigNumber, Is.EqualTo(long.MaxValue));
        Assert.That(p.Percent, Is.EqualTo(99.9999m));
        Assert.That(p.IsCool, Is.False);
        Assert.That(p.UniqueId, Is.EqualTo(new Guid(GuidString)));
        Assert.That(p.Url, Is.EqualTo(new Uri("http://example.com", UriKind.RelativeOrAbsolute)));
        Assert.That(p.UrlPath, Is.EqualTo(new Uri("/foo/bar", UriKind.RelativeOrAbsolute)));

        Assert.That(p.BestFriend, Is.Not.Null);
        Assert.That(p.BestFriend!.Name, Is.EqualTo("The Fonz"));
        Assert.That(p.BestFriend.Since, Is.EqualTo(1952));
    }

    [Test]
    public void Ignore_Protected_Property_That_Exists_In_Data()
    {
        var doc = CreateElementsXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.IgnoreProxy, Is.Null);
    }

    [Test]
    public void Ignore_ReadOnly_Property_That_Exists_In_Data()
    {
        var doc = CreateElementsXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.ReadOnlyProxy, Is.Null);
    }

    [Test]
    public void Can_Deserialize_Names_With_Underscores_On_Default_Root()
    {
        var doc = CreateUnderscoresXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Name, Is.EqualTo("John Sheehan"));
        Assert.That(p.StartDate, Is.EqualTo(new DateTime(2009, 9, 25, 0, 6, 1)));
        Assert.That(p.Age, Is.EqualTo(28));
        Assert.That(p.BigNumber, Is.EqualTo(long.MaxValue));
        Assert.That(p.Percent, Is.EqualTo(99.9999m));
        Assert.That(p.IsCool, Is.False);
        Assert.That(p.UniqueId, Is.EqualTo(new Guid(GuidString)));
        Assert.That(p.Url, Is.EqualTo(new Uri("http://example.com", UriKind.RelativeOrAbsolute)));
        Assert.That(p.UrlPath, Is.EqualTo(new Uri("/foo/bar", UriKind.RelativeOrAbsolute)));

        Assert.That(p.Friends, Is.Not.Null);
        Assert.That(p.Friends!.Count, Is.EqualTo(10));

        Assert.That(p.BestFriend, Is.Not.Null);
        Assert.That(p.BestFriend!.Name, Is.EqualTo("The Fonz"));
        Assert.That(p.BestFriend.Since, Is.EqualTo(1952));

        Assert.That(p.Foes, Is.Not.Null);
        Assert.That(p.Foes!.Count, Is.EqualTo(5));
        Assert.That(p.Foes.Team, Is.EqualTo("Yankees"));
    }

    [Test]
    public void Can_Deserialize_Names_With_Dashes_On_Default_Root()
    {
        var doc = CreateDashesXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer();
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Name, Is.EqualTo("John Sheehan"));
        Assert.That(p.StartDate, Is.EqualTo(new DateTime(2009, 9, 25, 0, 6, 1)));
        Assert.That(p.Age, Is.EqualTo(28));
        Assert.That(p.BigNumber, Is.EqualTo(long.MaxValue));
        Assert.That(p.Percent, Is.EqualTo(99.9999m));
        Assert.That(p.IsCool, Is.False);
        Assert.That(p.UniqueId, Is.EqualTo(new Guid(GuidString)));
        Assert.That(p.Url, Is.EqualTo(new Uri("http://example.com", UriKind.RelativeOrAbsolute)));
        Assert.That(p.UrlPath, Is.EqualTo(new Uri("/foo/bar", UriKind.RelativeOrAbsolute)));

        Assert.That(p.Friends, Is.Not.Null);
        Assert.That(p.Friends!.Count, Is.EqualTo(10));

        Assert.That(p.BestFriend, Is.Not.Null);
        Assert.That(p.BestFriend!.Name, Is.EqualTo("The Fonz"));
        Assert.That(p.BestFriend.Since, Is.EqualTo(1952));

        Assert.That(p.Foes, Is.Not.Null);
        Assert.That(p.Foes!.Count, Is.EqualTo(5));
        Assert.That(p.Foes.Team, Is.EqualTo("Yankees"));
    }

    [Test]
    public void Can_Deserialize_Boolean_From_Number()
    {
        var xmlpath = PathFor("boolean_from_number.xml");
        var doc = XDocument.Load(xmlpath);
        var response = new RestResponse { Content = doc.ToString() };

        var d = new XmlDeserializer();
        var output = d.Deserialize<BooleanTest>(response);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Value, Is.True);
    }

    [Test]
    public void Can_Deserialize_Boolean_From_String()
    {
        var xmlpath = PathFor("boolean_from_string.xml");
        var doc = XDocument.Load(xmlpath);
        var response = new RestResponse { Content = doc.ToString() };

        var d = new XmlDeserializer();
        var output = d.Deserialize<BooleanTest>(response);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Value, Is.True);
    }

    [Test]
    public void Can_Deserialize_Empty_Elements_With_Attributes_to_Nullable_Values()
    {
        var doc = CreateXmlWithAttributesAndNullValues();

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<NullableValues>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Null);
        Assert.That(output.StartDate, Is.Null);
        Assert.That(output.UniqueId, Is.Null);
    }

    [Test]
    public void Can_Deserialize_Mixture_Of_Empty_Elements_With_Attributes_And_Populated_Elements()
    {
        var doc = CreateXmlWithAttributesAndNullValuesAndPopulatedValues();

        var xml = new XmlDeserializer();
        var output = xml.Deserialize<NullableValues>(new RestResponse { Content = doc });

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Null);
        Assert.That(output.StartDate, Is.Null);
        Assert.That(output.UniqueId, Is.EqualTo(new Guid(GuidString)));
    }

    private static string CreateUnderscoresXml()
    {
        var doc = new XDocument();
        var root = new XElement("Person");
        root.Add(new XElement("Name", "John Sheehan"));
        root.Add(new XElement("Start_Date", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XAttribute("Age", 28));
        root.Add(new XElement("Percent", 99.9999m));
        root.Add(new XElement("Big_Number", long.MaxValue));
        root.Add(new XAttribute("Is_Cool", false));
        root.Add(new XElement("Ignore", "dummy"));
        root.Add(new XAttribute("Read_Only", "dummy"));
        root.Add(new XElement("Unique_Id", new Guid(GuidString)));
        root.Add(new XElement("Url", "http://example.com"));
        root.Add(new XElement("Url_Path", "/foo/bar"));

        root.Add(new XElement("Best_Friend",
            new XElement("Name", "The Fonz"),
            new XAttribute("Since", 1952)
        ));

        var friends = new XElement("Friends");
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new XElement("Friend",
                new XElement("Name", "Friend" + i),
                new XAttribute("Since", DateTime.Now.Year - i)
            ));
        }
        root.Add(friends);

        var foes = new XElement("Foes");
        foes.Add(new XAttribute("Team", "Yankees"));
        for (int i = 0; i < 5; i++)
        {
            foes.Add(new XElement("Foe", new XElement("Nickname", "Foe" + i)));
        }
        root.Add(foes);

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateDashesXml()
    {
        var doc = new XDocument();
        var root = new XElement("Person");
        root.Add(new XElement("Name", "John Sheehan"));
        root.Add(new XElement("Start_Date", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XAttribute("Age", 28));
        root.Add(new XElement("Percent", 99.9999m));
        root.Add(new XElement("Big-Number", long.MaxValue));
        root.Add(new XAttribute("Is-Cool", false));
        root.Add(new XElement("Ignore", "dummy"));
        root.Add(new XAttribute("Read-Only", "dummy"));
        root.Add(new XElement("Unique-Id", new Guid(GuidString)));
        root.Add(new XElement("Url", "http://example.com"));
        root.Add(new XElement("Url-Path", "/foo/bar"));

        root.Add(new XElement("Best-Friend",
            new XElement("Name", "The Fonz"),
            new XAttribute("Since", 1952)
        ));

        var friends = new XElement("Friends");
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new XElement("Friend",
                new XElement("Name", "Friend" + i),
                new XAttribute("Since", DateTime.Now.Year - i)
            ));
        }
        root.Add(friends);

        var foes = new XElement("Foes");
        foes.Add(new XAttribute("Team", "Yankees"));
        for (int i = 0; i < 5; i++)
        {
            foes.Add(new XElement("Foe", new XElement("Nickname", "Foe" + i)));
        }
        root.Add(foes);

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateElementsXml()
    {
        var doc = new XDocument();
        var root = new XElement("Person");
        root.Add(new XElement("Name", "John Sheehan"));
        root.Add(new XElement("StartDate", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XElement("Age", 28));
        root.Add(new XElement("Percent", 99.9999m));
        root.Add(new XElement("BigNumber", long.MaxValue));
        root.Add(new XElement("IsCool", false));
        root.Add(new XElement("Ignore", "dummy"));
        root.Add(new XElement("ReadOnly", "dummy"));
        root.Add(new XElement("UniqueId", new Guid(GuidString)));
        root.Add(new XElement("EmptyGuid", ""));
        root.Add(new XElement("Url", "http://example.com"));
        root.Add(new XElement("UrlPath", "/foo/bar"));
        root.Add(new XElement("Order", "third"));
        root.Add(new XElement("Disposition", "so-so"));

        root.Add(new XElement("BestFriend",
            new XElement("Name", "The Fonz"),
            new XElement("Since", 1952)
        ));

        var friends = new XElement("Friends");
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new XElement("Friend",
                new XElement("Name", "Friend" + i),
                new XElement("Since", DateTime.Now.Year - i)
            ));
        }
        root.Add(friends);

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateAttributesXml()
    {
        var doc = new XDocument();
        var root = new XElement("Person");
        root.Add(new XAttribute("Name", "John Sheehan"));
        root.Add(new XAttribute("StartDate", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XAttribute("Age", 28));
        root.Add(new XAttribute("Percent", 99.9999m));
        root.Add(new XAttribute("BigNumber", long.MaxValue));
        root.Add(new XAttribute("IsCool", false));
        root.Add(new XAttribute("Ignore", "dummy"));
        root.Add(new XAttribute("ReadOnly", "dummy"));
        root.Add(new XAttribute("UniqueId", new Guid(GuidString)));
        root.Add(new XAttribute("Url", "http://example.com"));
        root.Add(new XAttribute("UrlPath", "/foo/bar"));

        root.Add(new XElement("BestFriend",
            new XAttribute("Name", "The Fonz"),
            new XAttribute("Since", 1952)
        ));

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithNullValues()
    {
        var doc = new XDocument();
        var root = new XElement("NullableValues");
        root.Add(new XElement("Id", null),
            new XElement("StartDate", null),
            new XElement("UniqueId", null));
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithoutEmptyValues(CultureInfo culture)
    {
        var doc = new XDocument();
        var root = new XElement("NullableValues");
        root.Add(new XElement("Id", 123),
            new XElement("StartDate", new DateTime(2010, 2, 21, 9, 35, 00).ToString(culture)),
            new XElement("UniqueId", new Guid(GuidString)));
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithEmptyNestedList()
    {
        var doc = new XDocument();
        var root = new XElement("EmptyListSample");
        root.Add(new XElement("Images"));
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithEmptyInlineList()
    {
        var doc = new XDocument();
        var root = new XElement("EmptyListSample");
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithAttributesAndNullValues()
    {
        var doc = new XDocument();
        var root = new XElement("NullableValues");
        var idElement = new XElement("Id", null);
        idElement.SetAttributeValue("SomeAttribute", "SomeAttribute_Value");
        root.Add(idElement,
            new XElement("StartDate", null),
            new XElement("UniqueId", null));
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateXmlWithAttributesAndNullValuesAndPopulatedValues()
    {
        var doc = new XDocument();
        var root = new XElement("NullableValues");
        var idElement = new XElement("Id", null);
        idElement.SetAttributeValue("SomeAttribute", "SomeAttribute_Value");
        root.Add(idElement,
            new XElement("StartDate", null),
            new XElement("UniqueId", new Guid(GuidString)));
        doc.Add(root);
        return doc.ToString();
    }
}
