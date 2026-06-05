using Microsoft.Extensions.Options;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Configurations;

public sealed record NotionOptions
{
    public const string SectionName = "Notion";

    public required string AuthToken { get; init; }
    public required string DatabaseId { get; init; }
    public IEnumerable<NotionPropertyOptions> Properties { get; init; } = new List<NotionPropertyOptions>();
}

public sealed record NotionPropertyOptions
{
    public required string Name { get; init; }
    public required NotionPropertyType Type { get; init; }
    public required string? Value { get; init; }
}

public sealed class NotionOptionsValidator : IValidateOptions<NotionOptions>
{
    public ValidateOptionsResult Validate(string? name, NotionOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AuthToken))
            failures.Add($"{nameof(NotionOptions.AuthToken)} is required.");

        if (string.IsNullOrWhiteSpace(options.DatabaseId))
            failures.Add($"{nameof(NotionOptions.DatabaseId)} is required.");

        if (!options.Properties.Any())
            failures.Add($"{nameof(NotionOptions.Properties)} must contain at least one mapping.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
