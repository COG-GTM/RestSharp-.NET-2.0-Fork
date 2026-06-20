using System.Text.Json.Serialization;

namespace RestSharp.Tests.SampleClasses;

public class VenuesResponse
{
    [JsonPropertyName("groups")]
    public List<Group>? Groups { get; set; }
}

public class Group
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
