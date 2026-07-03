using EmailIntelligence.Domain.Entities.CosmosDocuments;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IFeedProfileService
{
    Task<ConfigurationResult<FeedProfile>> CreateFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default);
}
