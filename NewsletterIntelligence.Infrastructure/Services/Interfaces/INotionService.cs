using NewsletterIntelligence.Domain.Entities;

namespace NewsletterIntelligence.Infrastructure.Services.Interfaces;

public interface INotionService
{
    Task<IEnumerable<string>> CreatePage(IEnumerable<NotionPageDraft> emails);
}