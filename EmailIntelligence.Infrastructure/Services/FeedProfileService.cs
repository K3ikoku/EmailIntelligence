using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class FeedProfileService(
    IValidateOptions<FeedProfile> validator,
    IRepository<FeedProfile> repository,
    ILogger<FeedProfileService> logger) : IFeedProfileService
{
    public async Task<IEnumerable<FeedProfile>> GetAllFeedProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await repository.QueryAsync(x => x.Enabled, cancellationToken);
    }
    
    public async Task<ConfigurationResult<FeedProfile>> UpsertFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default)
    {
        if (feedProfile is null)
            return ConfigurationResult<FeedProfile>.Failure(["Feed profile is required."]);

        var validation = validator.Validate(null, feedProfile);
        if (validation.Failed)
            return ConfigurationResult<FeedProfile>.Failure(validation.Failures ?? []);

        var stored = await repository.UpsertAsync(feedProfile, cancellationToken);
        logger.LogInformation("Feed profile {FeedProfileId} upserted.", stored.Id);
        return ConfigurationResult<FeedProfile>.Success(stored);
    }

    public async Task<bool> DeleteFeedProfileAsync(string id, CancellationToken cancellationToken = default)
    {
        var matches = await repository.QueryAsync(profile => profile.Id == id, cancellationToken);
        var existing = matches.FirstOrDefault();
        if (existing is null)
            return false;

        await repository.DeleteAsync(existing.Id, existing.PartitionKey, cancellationToken);
        logger.LogInformation("Feed profile {FeedProfileId} deleted.", id);
        return true;
    }
}
