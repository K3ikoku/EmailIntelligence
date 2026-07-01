using Microsoft.Extensions.Options;

namespace EmailIntelligence.Domain.Entities.Configurations;

public sealed record ImapInputConfigurationRequest
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required bool UseSsl { get; init; }
    public required string RetrievingFolder { get; init; }
    public required string ProcessedFolder { get; init; }
}

public sealed class ImapInputConfigurationRequestValidator : IValidateOptions<ImapInputConfigurationRequest>
{
    public ValidateOptionsResult Validate(string? name, ImapInputConfigurationRequest options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
            failures.Add($"{nameof(ImapInputConfigurationRequest.Host)} is required.");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add($"{nameof(ImapInputConfigurationRequest.Username)} is required.");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add($"{nameof(ImapInputConfigurationRequest.Password)} is required.");

        if (string.IsNullOrWhiteSpace(options.RetrievingFolder))
            failures.Add($"{nameof(ImapInputConfigurationRequest.RetrievingFolder)} is required.");

        if (string.IsNullOrWhiteSpace(options.ProcessedFolder))
            failures.Add($"{nameof(ImapInputConfigurationRequest.ProcessedFolder)} is required.");

        if (options.Port is < 1 or > 65535)
            failures.Add($"{nameof(ImapInputConfigurationRequest.Port)} must be between 1 and 65535.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
