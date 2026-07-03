using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class FeedProfileService(
    IValidateOptions<FeedProfile> validator,
    IRepository<FeedProfile> repository) : IFeedProfileService
{
    public async Task<ConfigurationResult<FeedProfile>> CreateFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default)
    {
        if (feedProfile is null)
            return ConfigurationResult<FeedProfile>.Failure(["Feed profile is required."]);

        var validation = validator.Validate(null, feedProfile);
        if (validation.Failed)
            return ConfigurationResult<FeedProfile>.Failure(validation.Failures ?? []);

        var created = await repository.CreateAsync(feedProfile, cancellationToken);
        return ConfigurationResult<FeedProfile>.Success(created);
    }
}
