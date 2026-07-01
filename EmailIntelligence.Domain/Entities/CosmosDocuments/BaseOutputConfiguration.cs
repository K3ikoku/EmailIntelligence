using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

public abstract record BaseOutputConfiguration : Document
{
    public abstract OutputHost OutputHost { get; }
    [JsonIgnore]
    public override string PartitionKey => Id;
}