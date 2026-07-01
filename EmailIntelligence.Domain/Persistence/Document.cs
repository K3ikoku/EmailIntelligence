using System.Text.Json.Serialization;

namespace EmailIntelligence.Domain.Persistence;

public abstract record Document : IDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    [JsonIgnore]
    public abstract string PartitionKey { get; }

    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }
}
