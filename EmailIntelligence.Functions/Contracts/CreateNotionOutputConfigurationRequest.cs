using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateNotionOutputConfigurationRequest
{
    public string? Id { get; init; }
    public Guid AuthTokenId { get; private set; } = Guid.NewGuid();
    public string? AuthToken { get; init; }
    public IEnumerable<Page>? Pages { get; init; }

    public NotionOutputConfiguration ToConfiguration()
    {
        var configuration = new NotionOutputConfiguration
        {
            AuthTokenId = AuthTokenId,
            Pages = Pages ?? []
        };

        if (!string.IsNullOrWhiteSpace(Id))
            configuration.Id = Id;

        return configuration;
    }

    public IReadOnlyList<string> ValidateSecret() =>
        string.IsNullOrWhiteSpace(AuthToken)
            ? [$"{nameof(AuthToken)} is required."]
            : [];
}
