using EmailIntelligence.Domain.Entities.Configurations;

namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateImapInputConfigurationRequest
{
    public string? Id { get; init; }
    public string? Host { get; init; }
    public int Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
    public string? RetrievingFolder { get; init; }
    public string? ProcessedFolder { get; init; }

    public ImapInputConfiguration ToConfiguration()
    {
        var configuration = new ImapInputConfiguration
        {
            Host = Host ?? string.Empty,
            Port = Port,
            Username = Username ?? string.Empty,
            UseSsl = UseSsl,
            RetrievingFolder = RetrievingFolder ?? string.Empty,
            ProcessedFolder = ProcessedFolder ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(Id))
            configuration.Id = Id;

        return configuration;
    }

    public IReadOnlyList<string> ValidateSecret() =>
        string.IsNullOrWhiteSpace(Password)
            ? [$"{nameof(Password)} is required."]
            : [];
}
