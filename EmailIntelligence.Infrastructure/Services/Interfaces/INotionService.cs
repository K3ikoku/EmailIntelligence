using EmailIntelligence.Domain.Entities;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface INotionService
{
    Task<IEnumerable<string>> CreatePage(IEnumerable<NotionPageDraft> emails);
}