namespace NewsletterIntelligence.Domain.Configurations;

public sealed record ImapSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required bool UseSsl { get; init; }
    public required string RetrievingFolder { get; init; }
    public required string ProcessedFolder { get; init; }
}