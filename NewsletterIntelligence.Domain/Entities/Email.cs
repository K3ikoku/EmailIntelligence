using NewsletterIntelligence.Domain.Entities.Content;

namespace NewsletterIntelligence.Domain.Entities;

public sealed record Email
{
    public required string EmailSender { get; init; }
    public required string Subject { get; init; } //TODO: Do i want this? 
    public required ExtractedContent Content { get; init; }
    public required DateTimeOffset DateReceived { get; init; }
    public required string EmailUuid { get; init; }
    public required string RawBody { get; init; }
}