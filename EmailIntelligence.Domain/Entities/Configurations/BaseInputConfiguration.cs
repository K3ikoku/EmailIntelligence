using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Configurations;

public abstract record BaseInputConfiguration
{
    public abstract InputHost InputHost { get; }
}