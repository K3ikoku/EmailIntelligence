using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

public abstract record ConnectorConfiguration : Document
{
    public abstract Connector Connector { get; }
    public abstract ConnectorDirection Direction { get; }

    [JsonIgnore]
    public override string PartitionKey => Id;
}
