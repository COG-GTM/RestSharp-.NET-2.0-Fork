using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using NUnit.Framework;
using RestSharp.Serializers.Xml;
using RestSharp.Tests.SampleClasses;

namespace RestSharp.Tests;

[TestFixture]
public class SerializerTests
{
    [SetUp]
    public void Setup()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InstalledUICulture;
    }

    [Test]
    public void Can_Serialize_Simple_POCO_To_Xml()
    {
        var poco = new PersonForXml
        {
            Name = "Foo",
            Age = 50,
            Percent = 19.95m,
            StartDate = new DateTime(2009, 12, 18, 10, 2, 23)
        };

        var xml = new XmlSerializer();
        var doc = xml.Serialize(poco);

        Assert.That(doc, Is.Not.Null);
        Assert.That(doc, Does.Contain("<Name>Foo</Name>"));
        Assert.That(doc, Does.Contain("<Age>50</Age>"));
    }

    [Test]
    public void Can_Serialize_Simple_POCO_To_Json()
    {
        var poco = new PersonForJson
        {
            Name = "Foo",
            Age = 50,
            Percent = 19.95m,
            StartDate = new DateTime(2009, 12, 18, 10, 2, 23)
        };

        var json = JsonSerializer.Serialize(poco);

        Assert.That(json, Is.Not.Null);
        Assert.That(json, Does.Contain("\"Name\":\"Foo\""));
        Assert.That(json, Does.Contain("\"Age\":50"));
    }

    [Test]
    public void Can_Serialize_With_Custom_Root_Element()
    {
        var poco = new PersonForXml
        {
            Name = "Foo",
            Age = 50,
            Percent = 19.95m,
            StartDate = new DateTime(2009, 12, 18, 10, 2, 23)
        };

        var xml = new XmlSerializer { RootElement = "Result" };
        var doc = xml.Serialize(poco);

        Assert.That(doc, Is.Not.Null);
        Assert.That(doc, Does.Contain("<Result>"));
    }

    [Test]
    public void Can_Roundtrip_Json_Serialization()
    {
        var original = new PersonForJson
        {
            Name = "Test User",
            Age = 30,
            Percent = 50.5m,
            BigNumber = long.MaxValue,
            IsCool = true,
            Guid = Guid.NewGuid(),
            Url = new Uri("http://example.com"),
            Friends = new List<Friend>
            {
                new() { Name = "Friend1", Since = 2020 },
                new() { Name = "Friend2", Since = 2021 }
            },
            BestFriend = new Friend { Name = "BFF", Since = 2010 }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonForJson>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Name, Is.EqualTo(original.Name));
        Assert.That(deserialized.Age, Is.EqualTo(original.Age));
        Assert.That(deserialized.Percent, Is.EqualTo(original.Percent));
        Assert.That(deserialized.BigNumber, Is.EqualTo(original.BigNumber));
        Assert.That(deserialized.IsCool, Is.EqualTo(original.IsCool));
        Assert.That(deserialized.Guid, Is.EqualTo(original.Guid));
        Assert.That(deserialized.Friends!.Count, Is.EqualTo(2));
        Assert.That(deserialized.BestFriend!.Name, Is.EqualTo("BFF"));
    }

    [Test]
    public void Can_Serialize_Nullable_Values()
    {
        var nullable = new NullableValues
        {
            Id = 42,
            StartDate = new DateTime(2023, 1, 1),
            UniqueId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(nullable);
        var deserialized = JsonSerializer.Deserialize<NullableValues>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Id, Is.EqualTo(42));
        Assert.That(deserialized.StartDate, Is.EqualTo(new DateTime(2023, 1, 1)));
        Assert.That(deserialized.UniqueId, Is.EqualTo(nullable.UniqueId));
    }

    [Test]
    public void Can_Serialize_Null_Nullable_Values()
    {
        var nullable = new NullableValues
        {
            Id = null,
            StartDate = null,
            UniqueId = null
        };

        var json = JsonSerializer.Serialize(nullable);
        var deserialized = JsonSerializer.Deserialize<NullableValues>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Id, Is.Null);
        Assert.That(deserialized.StartDate, Is.Null);
        Assert.That(deserialized.UniqueId, Is.Null);
    }

    [Test]
    public void Can_Serialize_Dictionary()
    {
        var dict = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var json = JsonSerializer.Serialize(dict);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!["key1"], Is.EqualTo("value1"));
        Assert.That(deserialized["key2"], Is.EqualTo("value2"));
    }
}
