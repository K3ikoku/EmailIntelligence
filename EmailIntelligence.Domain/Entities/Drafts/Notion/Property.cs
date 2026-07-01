using Notion.Client;

namespace EmailIntelligence.Domain.Entities.Drafts.Notion;

public sealed record Property
{
    public required string Name { get; init; }
    public required PropertyValue Value { get; init; }
}