using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using RestSharp.Tests.SampleClasses;

namespace RestSharp.Tests;

[TestFixture]
public class JsonTests
{
    private const string GuidString = "AC1FC4BC-087A-4242-B8EE-C53EBE9887A5";

    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public void Can_Deserialize_4sq_Json_With_Root_Element_Specified()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "4sq.txt"));

        using var jsonDoc = JsonDocument.Parse(doc);
        var responseElement = jsonDoc.RootElement.GetProperty("response");
        var output = JsonSerializer.Deserialize<VenuesResponse>(responseElement.GetRawText(), DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Groups, Is.Not.Empty);
    }

    [Test]
    public void Can_Deserialize_Lists_of_Simple_Types()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "jsonlists.txt"));
        var output = JsonSerializer.Deserialize<JsonLists>(doc, DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Names, Is.Not.Empty);
        Assert.That(output.Numbers, Is.Not.Empty);
    }

    [Test]
    public void Can_Deserialize_Simple_Generic_List_of_Simple_Types()
    {
        const string content = "{\"users\":[\"johnsheehan\",\"jagregory\",\"drusellers\",\"structuremap\"]}";

        using var jsonDoc = JsonDocument.Parse(content);
        var usersElement = jsonDoc.RootElement.GetProperty("users");
        var output = JsonSerializer.Deserialize<List<string>>(usersElement.GetRawText(), DefaultOptions);

        Assert.That(output, Is.Not.Empty);
    }

    [Test]
    public void Can_Deserialize_From_Root_Element()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "sojson.txt"));

        using var jsonDoc = JsonDocument.Parse(doc);
        var userElement = jsonDoc.RootElement.GetProperty("User");
        var output = JsonSerializer.Deserialize<SOUser>(userElement.GetRawText(), DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.DisplayName, Is.EqualTo("John Sheehan"));
    }

    [Test]
    public void Can_Deserialize_Generic_Members()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "GenericWithList.txt"));
        var output = JsonSerializer.Deserialize<Generic<GenericWithList<Foe>>>(doc, DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Data, Is.Not.Null);
        Assert.That(output.Data!.Items, Is.Not.Null);
        Assert.That(output.Data.Items![0].Nickname, Is.EqualTo("Foe sho"));
    }

    [Test]
    public void Can_Deserialize_Empty_Elements_to_Nullable_Values()
    {
        var doc = CreateJsonWithNullValues();
        var output = JsonSerializer.Deserialize<NullableValues>(doc, DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Null);
        Assert.That(output.StartDate, Is.Null);
        Assert.That(output.UniqueId, Is.Null);
    }

    [Test]
    public void Can_Deserialize_Elements_to_Nullable_Values()
    {
        var doc = CreateJsonWithoutEmptyValues();
        var output = JsonSerializer.Deserialize<NullableValues>(doc, DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Id, Is.Not.Null);
        Assert.That(output.StartDate, Is.Not.Null);
        Assert.That(output.UniqueId, Is.Not.Null);

        Assert.That(output.Id, Is.EqualTo(123));
        Assert.That(output.UniqueId, Is.EqualTo(new Guid(GuidString)));
    }

    [Test]
    public void Can_Deserialize_Root_Json_Array_To_List()
    {
        var data = File.ReadAllText(Path.Combine("SampleData", "jsonarray.txt"));
        var output = JsonSerializer.Deserialize<List<status>>(data, DefaultOptions);

        Assert.That(output, Is.Not.Null);
        Assert.That(output!.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Deserialize_Guid_String_Fields()
    {
        var json = $"{{\"Guid\":\"{GuidString}\"}}";
        var p = JsonSerializer.Deserialize<PersonForJson>(json, DefaultOptions);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Guid, Is.EqualTo(new Guid(GuidString)));
    }

    [Test]
    public void Can_Deserialize_Quoted_Primitive()
    {
        var json = "{\"Age\":\"28\"}";
        var options = new JsonSerializerOptions(DefaultOptions)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        var p = JsonSerializer.Deserialize<PersonForJson>(json, options);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Age, Is.EqualTo(28));
    }

    [Test]
    public void Can_Deserialize_With_Default_Root()
    {
        var doc = CreateJson();
        var p = JsonSerializer.Deserialize<PersonForJson>(doc, DefaultOptions);

        Assert.That(p, Is.Not.Null);
        Assert.That(p!.Name, Is.EqualTo("John Sheehan"));
        Assert.That(p.Age, Is.EqualTo(28));
        Assert.That(p.BigNumber, Is.EqualTo(long.MaxValue));
        Assert.That(p.Percent, Is.EqualTo(99.9999m));
        Assert.That(p.IsCool, Is.False);
        Assert.That(p.Url, Is.EqualTo(new Uri("http://example.com", UriKind.RelativeOrAbsolute)));
        Assert.That(p.UrlPath, Is.EqualTo(new Uri("/foo/bar", UriKind.RelativeOrAbsolute)));
        Assert.That(p.Guid, Is.EqualTo(new Guid(GuidString)));
        Assert.That(p.Order, Is.EqualTo(Order.Third));
        Assert.That(p.Disposition, Is.EqualTo(Disposition.SoSo));

        Assert.That(p.Friends, Is.Not.Null);
        Assert.That(p.Friends!.Count, Is.EqualTo(10));

        Assert.That(p.BestFriend, Is.Not.Null);
        Assert.That(p.BestFriend!.Name, Is.EqualTo("The Fonz"));
        Assert.That(p.BestFriend.Since, Is.EqualTo(1952));

        Assert.That(p.Foes, Is.Not.Empty);
        Assert.That(p.Foes!["dict1"].Nickname, Is.EqualTo("Foe 1"));
        Assert.That(p.Foes["dict2"].Nickname, Is.EqualTo("Foe 2"));
    }

    [Test]
    public void Can_Deserialize_To_Dictionary_String_String()
    {
        var doc = CreateJsonStringDictionary();
        var bd = JsonSerializer.Deserialize<Dictionary<string, string>>(doc, DefaultOptions);

        Assert.That(bd, Is.Not.Null);
        Assert.That(bd!["Thing1"], Is.EqualTo("Thing1"));
        Assert.That(bd["Thing2"], Is.EqualTo("Thing2"));
        Assert.That(bd["ThingRed"], Is.EqualTo("ThingRed"));
        Assert.That(bd["ThingBlue"], Is.EqualTo("ThingBlue"));
    }

    [Test]
    public void Can_Deserialize_DateTime()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "datetimes.txt"));
        var payload = JsonSerializer.Deserialize<DateTimeTestStructure>(doc, DefaultOptions);

        Assert.That(payload, Is.Not.Null);
        Assert.That(
            payload!.DateTime.ToUniversalTime().ToString("u"),
            Is.EqualTo(new DateTime(2011, 6, 30, 8, 15, 46, DateTimeKind.Utc).ToString("u")));
    }

    [Test]
    public void Can_Deserialize_Nullable_DateTime_With_Value()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "datetimes.txt"));
        var payload = JsonSerializer.Deserialize<DateTimeTestStructure>(doc, DefaultOptions);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.NullableDateTimeWithValue, Is.Not.Null);
        Assert.That(
            payload.NullableDateTimeWithValue!.Value.ToUniversalTime().ToString("u"),
            Is.EqualTo(new DateTime(2011, 6, 30, 8, 15, 46, DateTimeKind.Utc).ToString("u")));
    }

    [Test]
    public void Can_Deserialize_Nullable_DateTime_With_Null()
    {
        var doc = File.ReadAllText(Path.Combine("SampleData", "datetimes.txt"));
        var payload = JsonSerializer.Deserialize<DateTimeTestStructure>(doc, DefaultOptions);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.NullableDateTimeWithNull, Is.Null);
    }

    private static string CreateJson()
    {
        var friends = new List<Friend>();
        for (int i = 0; i < 10; i++)
        {
            friends.Add(new Friend { Name = "Friend" + i, Since = DateTime.Now.Year - i });
        }

        var person = new PersonForJson
        {
            Name = "John Sheehan",
            StartDate = new DateTime(2009, 9, 25, 0, 6, 1, DateTimeKind.Utc),
            Age = 28,
            Percent = 99.9999m,
            BigNumber = long.MaxValue,
            IsCool = false,
            Url = new Uri("http://example.com"),
            UrlPath = new Uri("/foo/bar", UriKind.RelativeOrAbsolute),
            Guid = new Guid(GuidString),
            EmptyGuid = Guid.Empty,
            Order = Order.Third,
            Disposition = Disposition.SoSo,
            Friends = friends,
            BestFriend = new Friend { Name = "The Fonz", Since = 1952 },
            Foes = new Dictionary<string, Foe>
            {
                { "dict1", new Foe { Nickname = "Foe 1" } },
                { "dict2", new Foe { Nickname = "Foe 2" } }
            }
        };

        return JsonSerializer.Serialize(person, DefaultOptions);
    }

    private static string CreateJsonWithNullValues()
    {
        return JsonSerializer.Serialize(new NullableValues
        {
            Id = null,
            StartDate = null,
            UniqueId = null
        }, DefaultOptions);
    }

    private static string CreateJsonWithoutEmptyValues()
    {
        return JsonSerializer.Serialize(new NullableValues
        {
            Id = 123,
            StartDate = new DateTime(2010, 2, 21, 9, 35, 00, DateTimeKind.Utc),
            UniqueId = new Guid(GuidString)
        }, DefaultOptions);
    }

    private static string CreateJsonStringDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            { "Thing1", "Thing1" },
            { "Thing2", "Thing2" },
            { "ThingRed", "ThingRed" },
            { "ThingBlue", "ThingBlue" }
        };
        return JsonSerializer.Serialize(dict, DefaultOptions);
    }
}
