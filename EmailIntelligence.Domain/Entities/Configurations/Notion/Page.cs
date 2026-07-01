namespace EmailIntelligence.Domain.Entities.Configurations.Notion;

public sealed record Page
{
    public required string DatabaseId { get; init; }
    public required Property Title { get; init; }
    public required IEnumerable<Property> Properties { get; init; }
    public required IEnumerable<BaseInputConfiguration> InputConfigurations { get; init; }
}