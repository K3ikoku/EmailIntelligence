using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public record NotionOutputConfiguration : BaseOutputConfiguration
{
    public override OutputHost OutputHost => OutputHost.Notion;
    public required string AuthTokenId { get; init; }
    public required IEnumerable<Page> Pages { get; init; }
}