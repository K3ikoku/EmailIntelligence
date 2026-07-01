namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public sealed record Page : BaseConfiguration
{
    public required Property Title { get; init; }
    public required IEnumerable<Property> Properties { get; init; } = new List<Property>();
}