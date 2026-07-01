using EmailIntelligence.Domain.Entities.Drafts.Notion;

namespace EmailIntelligence.Infrastructure.Clients.Interfaces;

public interface INotionApiClient
{
    Task<bool> PageExists(string title);
    Task<string?> CreatePage(Page draft);
}