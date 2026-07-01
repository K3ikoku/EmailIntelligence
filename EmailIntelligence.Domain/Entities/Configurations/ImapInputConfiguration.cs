using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Configurations;

public record ImapInputConfiguration : BaseInputConfiguration
{
    public override InputHost InputHost => InputHost.Imap;
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required bool UseSsl { get; init; }
    public required string RetrievingFolder { get; init; }
    public required string ProcessedFolder { get; init; }
    public string ImapPasswordId => $"imap-{SanitizeSecretName(Username)}-password";

    private static string SanitizeSecretName(string value) =>
        new(value.Select(static c =>
            c is (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '-' ? c : '-').ToArray());
}