using Microsoft.Extensions.Options;

namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public sealed record NotionOutputConfigurationRequest
{
    public Guid AuthTokenId { get; init; } = Guid.NewGuid();
    public required string AuthToken { get; init; }
    public required IEnumerable<Page> Pages { get; init; }
}

public sealed class NotionOutputConfigurationRequestValidator : IValidateOptions<NotionOutputConfigurationRequest>
{
    public ValidateOptionsResult Validate(string? name, NotionOutputConfigurationRequest options)
    {
        var failures = new List<string>();

        if (options.AuthTokenId == Guid.Empty)
            failures.Add($"{nameof(NotionOutputConfigurationRequest.AuthTokenId)} is required.");

        if (string.IsNullOrWhiteSpace(options.AuthToken))
            failures.Add($"{nameof(NotionOutputConfigurationRequest.AuthToken)} is required.");

        if (options.Pages is null)
            failures.Add($"{nameof(NotionOutputConfigurationRequest.Pages)} is required.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
