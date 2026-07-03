using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class FeedProfileService(
    IValidateOptions<FeedProfile> validator,
    IRepository<FeedProfile> repository) : IFeedProfileService
{
    public async Task<ConfigurationResult<FeedProfile>> UpsertFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default)
    {
        if (feedProfile is null)
            return ConfigurationResult<FeedProfile>.Failure(["Feed profile is required."]);

        var validation = validator.Validate(null, feedProfile);
        if (validation.Failed)
            return ConfigurationResult<FeedProfile>.Failure(validation.Failures ?? []);

        var stored = await repository.UpsertAsync(feedProfile, cancellationToken);
        return ConfigurationResult<FeedProfile>.Success(stored);
    }

    public async Task<bool> DeleteFeedProfileAsync(string id, CancellationToken cancellationToken = default)
    {
        // Feed profiles partition by InputId, not id, so find the document to learn its partition key.
        var matches = await repository.QueryAsync(profile => profile.Id == id, cancellationToken);
        var existing = matches.FirstOrDefault();
        if (existing is null)
            return false;

        await repository.DeleteAsync(existing.Id, existing.PartitionKey, cancellationToken);
        return true;
    }
}
