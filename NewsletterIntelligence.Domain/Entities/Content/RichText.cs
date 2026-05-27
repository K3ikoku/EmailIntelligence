namespace NewsletterIntelligence.Domain.Entities.Content;

public sealed record RichText
{
    public required string Text { get; init; }
    public required string? Href { get; init; }
}