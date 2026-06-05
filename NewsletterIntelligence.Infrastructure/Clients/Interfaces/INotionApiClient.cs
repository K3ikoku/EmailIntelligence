using NewsletterIntelligence.Domain.Entities;

namespace NewsletterIntelligence.Infrastructure.Clients.Interfaces;

public interface INotionApiClient
{
    Task<bool> PageExists(string title);
    Task<string?> CreatePage(NotionPageDraft draft);
}