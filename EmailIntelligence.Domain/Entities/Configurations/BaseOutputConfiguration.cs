using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Configurations;

public abstract record BaseOutputConfiguration
{
    public abstract OutputHost OutputHost { get; }
}