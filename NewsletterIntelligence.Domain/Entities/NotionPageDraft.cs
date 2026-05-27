using Notion.Client;

namespace NewsletterIntelligence.Domain.Entities;

public sealed record NotionPageDraft
{
    public required IEnumerable<IBlock> Blocks { get; init; } = new List<IBlock>();
    public required Dictionary<string, object> Properties { get; init; } = new();
}