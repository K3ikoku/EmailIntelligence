using EmailIntelligence.Domain.Entities;

namespace EmailIntelligence.Infrastructure.Clients.Interfaces;

public interface INotionApiClient
{
    Task<bool> PageExists(string title);
    Task<string?> CreatePage(NotionPageDraft draft);
}