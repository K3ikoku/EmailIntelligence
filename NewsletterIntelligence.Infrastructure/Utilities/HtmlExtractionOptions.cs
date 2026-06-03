namespace NewsletterIntelligence.Infrastructure.Utilities;

public sealed record HtmlExtractionOptions
{
    public IReadOnlyCollection<string> IgnoreTextMarkers { get; init; } = ["(Sponsor)"];
    public IReadOnlyCollection<string> IgnoreElementIdsOrClasses { get; init; } = ["together-with"];
    public static HtmlExtractionOptions Default { get; } = new();
}
