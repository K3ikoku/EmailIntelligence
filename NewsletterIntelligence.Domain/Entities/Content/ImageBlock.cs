using NewsletterIntelligence.Domain.Enums;

namespace NewsletterIntelligence.Domain.Entities.Content;

public sealed record ImageBlock : ContentBlock
{
    public override ContentBlockType Type { get; init; } = ContentBlockType.Image;
    public required string Url { get; init; }
    public string? AltText { get; init; }
    public IReadOnlyList<RichText> Caption { get; init; } = new List<RichText>();
}