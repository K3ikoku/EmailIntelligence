namespace EmailIntelligence.Domain.Persistence;

/// <summary>
/// Contract for an entity stored in a partitioned document store (e.g. Azure Cosmos DB).
/// Kept provider-agnostic: it exposes the store's <c>id</c>, the logical partition-key
/// value, and an optimistic-concurrency token — without leaking any Cosmos types into
/// the domain. JSON property mapping (<c>id</c>, <c>_etag</c>) lives on <see cref="Document"/>.
/// </summary>
public interface IDocument
{
    /// <summary>Unique identifier within the partition (serialized as <c>id</c>).</summary>
    string Id { get; }

    /// <summary>
    /// The logical partition-key value for this item. The container defines the path
    /// (configured at registration time); this returns the value at that path.
    /// </summary>
    string PartitionKey { get; }

    /// <summary>
    /// Concurrency token assigned by the store. Pass it back on replace to get
    /// optimistic concurrency (the write fails if the stored item changed meanwhile).
    /// </summary>
    string? ETag { get; set; }
}
