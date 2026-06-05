using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Domain.Entities.Content;

public abstract record ContentBlock
{
    public abstract ContentBlockType Type { get; init; }
}
