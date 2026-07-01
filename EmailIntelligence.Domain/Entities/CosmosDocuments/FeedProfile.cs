using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;
using Newtonsoft.Json;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

public sealed record FeedProfile : Document
{
    public required string Name { get; init; }
    public bool Enabled { get; init; } = true;
    public required string InputId { get; init; }
    public required string OutputId { get; init; }
    public required IReadOnlyList<string> MatchRule { get; init; } //TODO: Revisit this
    public required IReadOnlyList<string> Processing { get; init; } //TODO: Revisit this
    public required Front Front { get; init; }
    
    [JsonIgnore]
    public override string PartitionKey => InputId;
}