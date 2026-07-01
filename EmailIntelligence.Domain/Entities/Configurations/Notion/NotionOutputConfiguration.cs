using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public record NotionOutputConfiguration : BaseOutputConfiguration
{
    public override OutputHost OutputHost => OutputHost.Notion;
    public required Guid AuthTokenId { get; init; }
    public required IEnumerable<Page> Pages { get; init; }
}