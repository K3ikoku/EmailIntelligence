using Notion.Client;

namespace NewsletterIntelligence.Domain.Entities;

public sealed record NotionPageDraft
{
    public required string EmailId { get; init; }
    public required string Title { get; init; }
    public required IEnumerable<IBlock> Blocks { get; init; } = new List<IBlock>();
    public required IEnumerable<NotionPageProperty> Properties { get; init; } = new List<NotionPageProperty>();
}