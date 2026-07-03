using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "connectorType")]
[JsonDerivedType(typeof(ImapInputConfiguration), "imap")]
[JsonDerivedType(typeof(NotionOutputConfiguration), "notion")]
public abstract record ConnectorConfiguration : Document
{
    public abstract Connector Connector { get; }
    public abstract ConnectorDirection Direction { get; }

    [JsonIgnore]
    public override string PartitionKey => Id;
}
