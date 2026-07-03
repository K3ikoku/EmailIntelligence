using EmailIntelligence.Domain.Entities.CosmosDocuments;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IFeedProfileService
{
    Task<ConfigurationResult<FeedProfile>> UpsertFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default);

    Task<bool> DeleteFeedProfileAsync(string id, CancellationToken cancellationToken = default);
}
