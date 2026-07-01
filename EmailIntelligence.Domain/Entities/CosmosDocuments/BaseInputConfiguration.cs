using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;
using Newtonsoft.Json;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

public abstract record BaseInputConfiguration : Document
{
    public abstract InputHost InputHost { get; }
    [JsonIgnore]
    public override string PartitionKey => Id;
}