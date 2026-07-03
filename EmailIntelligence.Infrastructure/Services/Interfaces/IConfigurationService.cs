using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IConfigurationService
{
    Task<ConfigurationResult<ImapInputConfiguration>> UpsertImapInputConfigurationAsync(
        ImapInputConfiguration configuration, string password, CancellationToken cancellationToken = default);

    Task<ConfigurationResult<NotionOutputConfiguration>> UpsertNotionOutputConfigurationAsync(
        NotionOutputConfiguration configuration, string authToken, CancellationToken cancellationToken = default);

    Task<bool> DeleteConnectorAsync(string id, CancellationToken cancellationToken = default);
}
