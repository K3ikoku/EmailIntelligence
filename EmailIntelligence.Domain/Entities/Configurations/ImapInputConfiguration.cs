using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Domain.Entities.Configurations;

public sealed record ImapInputConfiguration : BaseInputConfiguration
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

public sealed class ImapInputConfigurationValidator : IValidateOptions<ImapInputConfiguration>
{
    public ValidateOptionsResult Validate(string? name, ImapInputConfiguration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
            failures.Add($"{nameof(ImapInputConfiguration.Host)} is required.");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add($"{nameof(ImapInputConfiguration.Username)} is required.");

        if (string.IsNullOrWhiteSpace(options.RetrievingFolder))
            failures.Add($"{nameof(ImapInputConfiguration.RetrievingFolder)} is required.");

        if (string.IsNullOrWhiteSpace(options.ProcessedFolder))
            failures.Add($"{nameof(ImapInputConfiguration.ProcessedFolder)} is required.");

        if (options.Port is < 1 or > 65535)
            failures.Add($"{nameof(ImapInputConfiguration.Port)} must be between 1 and 65535.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
