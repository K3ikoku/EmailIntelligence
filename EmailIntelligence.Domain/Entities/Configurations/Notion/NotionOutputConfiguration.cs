using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public sealed record NotionOutputConfiguration : ConnectorConfiguration
{
    public override Connector Connector => Connector.Notion;
    public override ConnectorDirection Direction => ConnectorDirection.Output;
    public required Guid AuthTokenId { get; init; }
    public required IEnumerable<Page> Pages { get; init; }
}

public sealed class NotionOutputConfigurationValidator : IValidateOptions<NotionOutputConfiguration>
{
    public ValidateOptionsResult Validate(string? name, NotionOutputConfiguration options)
    {
        var failures = new List<string>();

        if (options.AuthTokenId == Guid.Empty)
            failures.Add($"{nameof(NotionOutputConfiguration.AuthTokenId)} is required.");

        if (options.Pages is null)
            failures.Add($"{nameof(NotionOutputConfiguration.Pages)} is required.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
