namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateImapInputConfigurationRequest
{
    public string? Host { get; init; }
    public int Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
    public string? RetrievingFolder { get; init; }
    public string? ProcessedFolder { get; init; }
}
