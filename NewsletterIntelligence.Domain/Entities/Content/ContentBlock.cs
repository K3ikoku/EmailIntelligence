using NewsletterIntelligence.Domain.Enums;

namespace NewsletterIntelligence.Domain.Entities.Content;

public abstract record ContentBlock
{
    public abstract ContentBlockType Type { get; init; }
}
