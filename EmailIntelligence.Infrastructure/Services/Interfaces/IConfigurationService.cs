using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IConfigurationService
{
    Task<ConfigurationResult<ImapInputConfiguration>> CreateImapInputConfigurationAsync(
        ImapInputConfigurationRequest request, CancellationToken cancellationToken = default);

    Task<ConfigurationResult<NotionOutputConfiguration>> CreateNotionOutputConfigurationAsync(
        NotionOutputConfigurationRequest request, CancellationToken cancellationToken = default);
}
