using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Infrastructure.Secrets;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Services;

public sealed class ConfigurationService(
    ISecretStore secretStore,
    IValidateOptions<ImapInputConfigurationRequest> imapValidator,
    IValidateOptions<NotionOutputConfigurationRequest> notionValidator) : IConfigurationService
{
    public async Task<ConfigurationResult<ImapInputConfiguration>> CreateImapInputConfigurationAsync(
        ImapInputConfigurationRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return ConfigurationResult<ImapInputConfiguration>.Failure(["Request is required."]);

        var validation = imapValidator.Validate(null, request);
        if (validation.Failed)
            return ConfigurationResult<ImapInputConfiguration>.Failure(validation.Failures ?? []);

        var configuration = new ImapInputConfiguration
        {
            Host = request.Host,
            Port = request.Port,
            Username = request.Username,
            UseSsl = request.UseSsl,
            RetrievingFolder = request.RetrievingFolder,
            ProcessedFolder = request.ProcessedFolder
        };

        await secretStore.SetSecretAsync(configuration.ImapPasswordId, request.Password, cancellationToken);
        return ConfigurationResult<ImapInputConfiguration>.Success(configuration);
    }

    public async Task<ConfigurationResult<NotionOutputConfiguration>> CreateNotionOutputConfigurationAsync(
        NotionOutputConfigurationRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return ConfigurationResult<NotionOutputConfiguration>.Failure(["Request is required."]);

        var validation = notionValidator.Validate(null, request);
        if (validation.Failed)
            return ConfigurationResult<NotionOutputConfiguration>.Failure(validation.Failures ?? []);

        var configuration = new NotionOutputConfiguration
        {
            AuthTokenId = request.AuthTokenId,
            Pages = request.Pages
        };

        await secretStore.SetSecretAsync(configuration.AuthTokenId.ToString(), request.AuthToken, cancellationToken);
        return ConfigurationResult<NotionOutputConfiguration>.Success(configuration);
    }
}
