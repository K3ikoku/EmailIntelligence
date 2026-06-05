using System.Text.Json.Serialization;

namespace EmailIntelligence.Domain.Persistence;

/// <summary>
/// Base class for <see cref="IDocument"/> entities. Carries the JSON mapping the store
/// requires (<c>id</c> and the <c>_etag</c> concurrency token) and a sensible default id.
/// </summary>
/// <remarks>
/// <see cref="PartitionKey"/> is intentionally <see cref="JsonIgnoreAttribute"/>'d: it is a
/// logical accessor used by the repository to route the write, and should point at an
/// already-serialized field (e.g. <c>sender</c>) rather than duplicating a value. Concrete
/// types must also annotate their override with <c>[JsonIgnore]</c> (STJ does not inherit
/// the attribute onto overrides).
/// </remarks>
public abstract class Document : IDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    [JsonIgnore]
    public abstract string PartitionKey { get; }

    [JsonPropertyName("_etag")]
    public string? ETag { get; set; }
}
