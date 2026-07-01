using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateNotionOutputConfigurationRequest
{
    public Guid AuthTokenId { get; private set; } = Guid.NewGuid();
    public string? AuthToken { get; init; }
    public IEnumerable<Page>? Pages { get; init; }
}
