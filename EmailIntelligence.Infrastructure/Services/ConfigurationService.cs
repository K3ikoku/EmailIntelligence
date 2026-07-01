using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Infrastructure.Secrets;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class ConfigurationService(
    ISecretStore secretStore,
    IValidateOptions<ImapInputConfiguration> imapValidator,
    IValidateOptions<NotionOutputConfiguration> notionValidator) : IConfigurationService
{
    public async Task<ConfigurationResult<ImapInputConfiguration>> CreateImapInputConfigurationAsync(
        ImapInputConfiguration configuration,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
            return ConfigurationResult<ImapInputConfiguration>.Failure(["Configuration is required."]);

        var validation = imapValidator.Validate(null, configuration);
        if (validation.Failed)
            return ConfigurationResult<ImapInputConfiguration>.Failure(validation.Failures ?? []);

        await secretStore.SetSecretAsync(configuration.ImapPasswordId, password, cancellationToken);
        return ConfigurationResult<ImapInputConfiguration>.Success(configuration);
    }

    public async Task<ConfigurationResult<NotionOutputConfiguration>> CreateNotionOutputConfigurationAsync(
        NotionOutputConfiguration configuration,
        string authToken,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
            return ConfigurationResult<NotionOutputConfiguration>.Failure(["Configuration is required."]);

        var validation = notionValidator.Validate(null, configuration);
        if (validation.Failed)
            return ConfigurationResult<NotionOutputConfiguration>.Failure(validation.Failures ?? []);

        await secretStore.SetSecretAsync(configuration.AuthTokenId.ToString(), authToken, cancellationToken);
        return ConfigurationResult<NotionOutputConfiguration>.Success(configuration);
    }
}
