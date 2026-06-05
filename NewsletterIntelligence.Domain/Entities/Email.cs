namespace NewsletterIntelligence.Domain.Entities;

public sealed record Email
{
    public required string EmailSender { get; init; }
    public required string Subject { get; init; }
    public required string EmailBody { get; init; }
    public required DateTimeOffset DateReceived { get; init; }
    public required string MessageId { get; init; }
}