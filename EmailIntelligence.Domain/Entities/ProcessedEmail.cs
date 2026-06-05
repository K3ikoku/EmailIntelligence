using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Domain.Entities;

/// <summary>
/// Sample Cosmos document: a record that a given email was processed into Notion.
/// Partitioned by <see cref="Sender"/>. Ready to use via <c>IRepository&lt;ProcessedEmail&gt;</c>
/// once a container is registered (see <c>AddCosmosContainer&lt;ProcessedEmail&gt;</c>).
/// </summary>
public sealed class ProcessedEmail : Document
{
    /// <summary>The newsletter/source the email came from; also the partition key.</summary>
    public required string Sender { get; init; }

    public required string Subject { get; init; }

    public required DateTimeOffset ProcessedAtUtc { get; init; }

    /// <summary>The Notion page created for this email, if any.</summary>
    public string? NotionPageId { get; init; }

    [JsonIgnore]
    public override string PartitionKey => Sender;
}
