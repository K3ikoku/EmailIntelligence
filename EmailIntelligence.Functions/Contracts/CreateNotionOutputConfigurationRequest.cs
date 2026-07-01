using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateNotionOutputConfigurationRequest
{
    public Guid AuthTokenId { get; private set; } = Guid.NewGuid();
    public string? AuthToken { get; init; }
    public IEnumerable<Page>? Pages { get; init; }

    public NotionOutputConfiguration ToConfiguration() => new()
    {
        AuthTokenId = AuthTokenId,
        Pages = Pages ?? []
    };

    public IReadOnlyList<string> ValidateSecret() =>
        string.IsNullOrWhiteSpace(AuthToken)
            ? [$"{nameof(AuthToken)} is required."]
            : [];
}
