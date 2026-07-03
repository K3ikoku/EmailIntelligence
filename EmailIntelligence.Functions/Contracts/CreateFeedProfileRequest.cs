using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Functions.Contracts;

public sealed class CreateFeedProfileRequest
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public bool Enabled { get; init; } = true;
    public string? InputId { get; init; }
    public string? OutputId { get; init; }
    public IReadOnlyList<string>? MatchRule { get; init; }
    public IReadOnlyList<string>? Processing { get; init; }
    public Front Front { get; init; }

    public FeedProfile ToFeedProfile()
    {
        var feedProfile = new FeedProfile
        {
            Name = Name ?? string.Empty,
            Enabled = Enabled,
            InputId = InputId ?? string.Empty,
            OutputId = OutputId ?? string.Empty,
            MatchRule = MatchRule ?? [],
            Processing = Processing ?? [],
            Front = Front
        };

        if (!string.IsNullOrWhiteSpace(Id))
            feedProfile.Id = Id;

        return feedProfile;
    }
}
