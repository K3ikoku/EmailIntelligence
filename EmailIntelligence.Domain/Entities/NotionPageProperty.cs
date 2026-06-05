using Notion.Client;

namespace EmailIntelligence.Domain.Entities;

public sealed record NotionPageProperty
{
    public required string Name { get; init; }
    public required PropertyValue Value { get; init; }
}