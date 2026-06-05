namespace EmailIntelligence.Domain.Entities.Content;

public sealed record ExtractedContent
{
    public IReadOnlyList<ContentBlock> Blocks { get; init; } = new List<ContentBlock>();
}