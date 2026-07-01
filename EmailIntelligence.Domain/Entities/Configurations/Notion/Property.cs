using Notion.Client;

namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public class Property
{
    public required string Name { get; init; }
    public required PropertyValueType Type { get; init; }
    public required string DefaultValue { get; init; }
}