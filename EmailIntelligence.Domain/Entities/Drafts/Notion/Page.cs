using Notion.Client;

namespace EmailIntelligence.Domain.Entities.Drafts.Notion;

public sealed record Page
{
    public required string EmailId { get; init; }
    public required string Title { get; init; }
    public required IEnumerable<IBlock> Blocks { get; init; } = new List<IBlock>();
    public required IEnumerable<Property> Properties { get; init; } = new List<Property>();
}