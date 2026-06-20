using System.Xml.Linq;
using NUnit.Framework;
using RestSharp.Serializers.Xml;
using RestSharp.Tests.SampleClasses;
using RestSharp.Tests.SampleClasses.Lastfm;

namespace RestSharp.Tests;

[TestFixture]
public class NamespacedXmlTests
{
    private const string GuidString = "AC1FC4BC-087A-4242-B8EE-C53EBE9887A5";

    [Test]
    public void Can_Deserialize_Elements_With_Namespace()
    {
        var doc = CreateElementsXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
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
    }

    [Test]
    public void Can_Deserialize_Attributes_With_Namespace()
    {
        var doc = CreateAttributesXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
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

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.IgnoreProxy, Is.Null);
    }

    [Test]
    public void Ignore_ReadOnly_Property_That_Exists_In_Data()
    {
        var doc = CreateElementsXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
        var p = d.Deserialize<PersonForXml>(response);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.ReadOnlyProxy, Is.Null);
    }

    [Test]
    public void Can_Deserialize_Names_With_Underscores_With_Namespace()
    {
        var doc = CreateUnderscoresXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
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
    public void Can_Deserialize_List_Of_Primitives_With_Namespace()
    {
        var doc = CreateListOfPrimitivesXml();
        var response = new RestResponse { Content = doc };

        var d = new XmlDeserializer { Namespace = "http://restsharp.org" };
        var a = d.Deserialize<List<artist>>(response);

        Assert.That(a, Is.Not.Null);
        Assert.That(a!.Count, Is.EqualTo(2));
        Assert.That(a[0].Value, Is.EqualTo("first"));
        Assert.That(a[1].Value, Is.EqualTo("second"));
    }

    private static string CreateListOfPrimitivesXml()
    {
        var doc = new XDocument();
        var ns = XNamespace.Get("http://restsharp.org");
        var root = new XElement(ns + "artists");
        root.Add(new XElement(ns + "artist", "first"));
        root.Add(new XElement(ns + "artist", "second"));
        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateUnderscoresXml()
    {
        var doc = new XDocument();
        var ns = XNamespace.Get("http://restsharp.org");
        var root = new XElement(ns + "Person");
        root.Add(new XElement(ns + "Name", "John Sheehan"));
        root.Add(new XElement(ns + "Start_Date", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XAttribute(ns + "Age", 28));
        root.Add(new XElement(ns + "Percent", 99.9999m));
        root.Add(new XElement(ns + "Big_Number", long.MaxValue));
        root.Add(new XAttribute(ns + "Is_Cool", false));
        root.Add(new XElement(ns + "Ignore", "dummy"));
        root.Add(new XAttribute(ns + "Read_Only", "dummy"));
        root.Add(new XAttribute(ns + "Unique_Id", new Guid(GuidString)));
        root.Add(new XElement(ns + "Url", "http://example.com"));
        root.Add(new XElement(ns + "Url_Path", "/foo/bar"));

        root.Add(new XElement(ns + "Best_Friend",
            new XElement(ns + "Name", "The Fonz"),
            new XAttribute(ns + "Since", 1952)
        ));

        var friends = new XElement(ns + "Friends");
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new XElement(ns + "Friend",
                new XElement(ns + "Name", "Friend" + i),
                new XAttribute(ns + "Since", DateTime.Now.Year - i)
            ));
        }
        root.Add(friends);

        var foes = new XElement(ns + "Foes");
        foes.Add(new XAttribute(ns + "Team", "Yankees"));
        for (int i = 0; i < 5; i++)
        {
            foes.Add(new XElement(ns + "Foe", new XElement(ns + "Nickname", "Foe" + i)));
        }
        root.Add(foes);

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateElementsXml()
    {
        var doc = new XDocument();
        var ns = XNamespace.Get("http://restsharp.org");
        var root = new XElement(ns + "Person");
        root.Add(new XElement(ns + "Name", "John Sheehan"));
        root.Add(new XElement(ns + "StartDate", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XElement(ns + "Age", 28));
        root.Add(new XElement(ns + "Percent", 99.9999m));
        root.Add(new XElement(ns + "BigNumber", long.MaxValue));
        root.Add(new XElement(ns + "IsCool", false));
        root.Add(new XElement(ns + "Ignore", "dummy"));
        root.Add(new XElement(ns + "ReadOnly", "dummy"));
        root.Add(new XElement(ns + "UniqueId", new Guid(GuidString)));
        root.Add(new XElement(ns + "Url", "http://example.com"));
        root.Add(new XElement(ns + "UrlPath", "/foo/bar"));

        root.Add(new XElement(ns + "BestFriend",
            new XElement(ns + "Name", "The Fonz"),
            new XElement(ns + "Since", 1952)
        ));

        var friends = new XElement(ns + "Friends");
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new XElement(ns + "Friend",
                new XElement(ns + "Name", "Friend" + i),
                new XElement(ns + "Since", DateTime.Now.Year - i)
            ));
        }
        root.Add(friends);

        doc.Add(root);
        return doc.ToString();
    }

    private static string CreateAttributesXml()
    {
        var doc = new XDocument();
        var ns = XNamespace.Get("http://restsharp.org");
        var root = new XElement(ns + "Person");
        root.Add(new XAttribute(ns + "Name", "John Sheehan"));
        root.Add(new XAttribute(ns + "StartDate", new DateTime(2009, 9, 25, 0, 6, 1)));
        root.Add(new XAttribute(ns + "Age", 28));
        root.Add(new XAttribute(ns + "Percent", 99.9999m));
        root.Add(new XAttribute(ns + "BigNumber", long.MaxValue));
        root.Add(new XAttribute(ns + "IsCool", false));
        root.Add(new XAttribute(ns + "Ignore", "dummy"));
        root.Add(new XAttribute(ns + "ReadOnly", "dummy"));
        root.Add(new XAttribute(ns + "UniqueId", new Guid(GuidString)));
        root.Add(new XAttribute(ns + "Url", "http://example.com"));
        root.Add(new XAttribute(ns + "UrlPath", "/foo/bar"));

        root.Add(new XElement(ns + "BestFriend",
            new XAttribute(ns + "Name", "The Fonz"),
            new XAttribute(ns + "Since", 1952)
        ));

        doc.Add(root);
        return doc.ToString();
    }
}
