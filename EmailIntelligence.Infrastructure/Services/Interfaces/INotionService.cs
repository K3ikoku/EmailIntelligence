using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Entities.Drafts.Notion;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface INotionService
{
    Task<IEnumerable<string>> CreatePage(IEnumerable<Page> emails);
}