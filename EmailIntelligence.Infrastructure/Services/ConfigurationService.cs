using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Secrets;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class ConfigurationService(
    ISecretStore secretStore,
    IRepository<ConnectorConfiguration> connectors,
    IValidateOptions<ImapInputConfiguration> imapValidator,
    IValidateOptions<NotionOutputConfiguration> notionValidator) : IConfigurationService
{
    public async Task<ConfigurationResult<ImapInputConfiguration>> UpsertImapInputConfigurationAsync(
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
        await connectors.UpsertAsync(configuration, cancellationToken);
        return ConfigurationResult<ImapInputConfiguration>.Success(configuration);
    }

    public async Task<ConfigurationResult<NotionOutputConfiguration>> UpsertNotionOutputConfigurationAsync(
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
        await connectors.UpsertAsync(configuration, cancellationToken);
        return ConfigurationResult<NotionOutputConfiguration>.Success(configuration);
    }

    public async Task<bool> DeleteConnectorAsync(string id, CancellationToken cancellationToken = default)
    {
        var connector = await connectors.GetAsync(id, id, cancellationToken);
        if (connector is null)
            return false;

        var secretName = connector switch
        {
            ImapInputConfiguration imap => imap.ImapPasswordId,
            NotionOutputConfiguration notion => notion.AuthTokenId.ToString(),
            _ => null
        };

        if (secretName is not null)
            await secretStore.DeleteSecretAsync(secretName, cancellationToken);

        await connectors.DeleteAsync(id, id, cancellationToken);
        return true;
    }
}
