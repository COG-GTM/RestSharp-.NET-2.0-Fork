using System.Text.Json.Serialization;

namespace RestSharp.Tests.SampleClasses;

public class SOUser
{
    [JsonPropertyName("user_id")]
    public int Id { get; set; }

    [JsonPropertyName("reputation")]
    public int Reputation { get; set; }

    [JsonPropertyName("creation_date")]
    public long CreationDate { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("email_hash")]
    public string? EmailHash { get; set; }

    [JsonPropertyName("age")]
    public string? Age { get; set; }

    [JsonPropertyName("last_access_date")]
    public long LastAccessDate { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("about_me")]
    public string? AboutMe { get; set; }

    [JsonPropertyName("view_count")]
    public int Views { get; set; }

    [JsonPropertyName("up_vote_count")]
    public int UpVotes { get; set; }

    [JsonPropertyName("down_vote_count")]
    public int DownVotes { get; set; }
}
