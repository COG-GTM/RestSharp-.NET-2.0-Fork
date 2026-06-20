using System.Text.Json.Serialization;

namespace RestSharp.Tests.SampleClasses;

public class PersonForXml
{
    public string? Name { get; set; }
    public DateTime StartDate { get; set; }
    public int Age { get; set; }
    public decimal Percent { get; set; }
    public long BigNumber { get; set; }
    public bool IsCool { get; set; }
    public List<Friend>? Friends { get; set; }
    public Friend? BestFriend { get; set; }

    protected string? Ignore { get; set; }
    public string? IgnoreProxy => Ignore;

    protected string? ReadOnly => null;
    public string? ReadOnlyProxy => ReadOnly;

    public FoeList? Foes { get; set; }

    public Guid UniqueId { get; set; }
    public Guid EmptyGuid { get; set; }

    public Uri? Url { get; set; }
    public Uri? UrlPath { get; set; }

    public Order Order { get; set; }

    public Disposition Disposition { get; set; }
}

public class PersonForJson
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("Age")]
    public int Age { get; set; }

    [JsonPropertyName("Percent")]
    public decimal Percent { get; set; }

    [JsonPropertyName("BigNumber")]
    public long BigNumber { get; set; }

    [JsonPropertyName("IsCool")]
    public bool IsCool { get; set; }

    [JsonPropertyName("Friends")]
    public List<Friend>? Friends { get; set; }

    [JsonPropertyName("BestFriend")]
    public Friend? BestFriend { get; set; }

    [JsonPropertyName("Guid")]
    public Guid Guid { get; set; }

    [JsonPropertyName("EmptyGuid")]
    public Guid EmptyGuid { get; set; }

    [JsonPropertyName("Url")]
    public Uri? Url { get; set; }

    [JsonPropertyName("UrlPath")]
    public Uri? UrlPath { get; set; }

    [JsonIgnore]
    protected string? Ignore { get; set; }
    [JsonIgnore]
    public string? IgnoreProxy => Ignore;

    [JsonIgnore]
    protected string? ReadOnly => null;
    [JsonIgnore]
    public string? ReadOnlyProxy => ReadOnly;

    [JsonPropertyName("Foes")]
    public Dictionary<string, Foe>? Foes { get; set; }

    [JsonPropertyName("Order")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Order Order { get; set; }

    [JsonPropertyName("Disposition")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition Disposition { get; set; }
}

public enum Order
{
    First,
    Second,
    Third
}

public enum Disposition
{
    Friendly,
    SoSo,
    SteerVeryClear
}

public class Friend
{
    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Since")]
    public int Since { get; set; }
}

public class Foe
{
    [JsonPropertyName("Nickname")]
    public string? Nickname { get; set; }
}

public class FoeList : List<Foe>
{
    public string? Team { get; set; }
}

public class Birthdate
{
    [JsonPropertyName("Value")]
    public DateTime Value { get; set; }
}

public class OrderedProperties
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public DateTime StartDate { get; set; }
}

public class DatabaseCollection : List<Database>
{
}

public class Database
{
    public string? Name { get; set; }
    public string? InitialCatalog { get; set; }
    public string? DataSource { get; set; }
}

public class Generic<T>
{
    public T? Data { get; set; }
}

public class GenericWithList<T>
{
    public List<T>? Items { get; set; }
}

public class DateTimeTestStructure
{
    [JsonPropertyName("DateTime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("NullableDateTimeWithNull")]
    public DateTime? NullableDateTimeWithNull { get; set; }

    [JsonPropertyName("NullableDateTimeWithValue")]
    public DateTime? NullableDateTimeWithValue { get; set; }

    [JsonPropertyName("DateTimeOffset")]
    public DateTimeOffset DateTimeOffset { get; set; }

    [JsonPropertyName("NullableDateTimeOffsetWithNull")]
    public DateTimeOffset? NullableDateTimeOffsetWithNull { get; set; }

    [JsonPropertyName("NullableDateTimeOffsetWithValue")]
    public DateTimeOffset? NullableDateTimeOffsetWithValue { get; set; }
}

public class JsonEnumsTestStructure
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition Upper { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition Lower { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition CamelCased { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition Underscores { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition LowerUnderscores { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition Dashes { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Disposition LowerDashes { get; set; }
}

public class NullableValues
{
    public int? Id { get; set; }
    public DateTime? StartDate { get; set; }
    public Guid? UniqueId { get; set; }
}
