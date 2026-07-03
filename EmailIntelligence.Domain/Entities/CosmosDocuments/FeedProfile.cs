using System.Text.Json.Serialization;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Domain.Entities.CosmosDocuments;

public sealed record FeedProfile : Document
{
    public required string Name { get; init; }
    public bool Enabled { get; init; } = true;
    public required string InputId { get; init; }
    public required string OutputId { get; init; }
    public required IReadOnlyList<string> MatchRule { get; init; } //TODO: Revisit this
    public required IReadOnlyList<string> Processing { get; init; } //TODO: Revisit this
    public required Front Front { get; init; }
    
    [JsonIgnore]
    public override string PartitionKey => InputId;
}

public sealed class FeedProfileValidator : IValidateOptions<FeedProfile>
{
    public ValidateOptionsResult Validate(string? name, FeedProfile options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Name))
            failures.Add($"{nameof(FeedProfile.Name)} is required.");

        if (string.IsNullOrWhiteSpace(options.InputId))
            failures.Add($"{nameof(FeedProfile.InputId)} is required.");

        if (string.IsNullOrWhiteSpace(options.OutputId))
            failures.Add($"{nameof(FeedProfile.OutputId)} is required.");

        if (!Enum.IsDefined(options.Front))
            failures.Add($"{nameof(FeedProfile.Front)} is not a valid value.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}