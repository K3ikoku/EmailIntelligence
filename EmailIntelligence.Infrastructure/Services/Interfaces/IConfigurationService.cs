using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IConfigurationService
{
    Task<ConfigurationResult<ImapInputConfiguration>> CreateImapInputConfigurationAsync(
        ImapInputConfiguration configuration, string password, CancellationToken cancellationToken = default);

    Task<ConfigurationResult<NotionOutputConfiguration>> CreateNotionOutputConfigurationAsync(
        NotionOutputConfiguration configuration, string authToken, CancellationToken cancellationToken = default);
}
