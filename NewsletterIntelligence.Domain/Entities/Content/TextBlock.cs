using NewsletterIntelligence.Domain.Enums;

namespace NewsletterIntelligence.Domain.Entities.Content;

public sealed record TextBlock : ContentBlock
{
    public override required ContentBlockType Type { get; init; }
    public required IReadOnlyList<RichText> RichText { get; init; } = new List<RichText>();
}