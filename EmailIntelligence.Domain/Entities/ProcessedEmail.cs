using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Domain.Entities;

public sealed class ProcessedEmail : Document
{
    public required string Sender { get; init; }

    public required string Subject { get; init; }

    public required DateTimeOffset ProcessedAtUtc { get; init; }

    public string? NotionPageId { get; init; }

    [JsonIgnore]
    public override string PartitionKey => Sender;
}
