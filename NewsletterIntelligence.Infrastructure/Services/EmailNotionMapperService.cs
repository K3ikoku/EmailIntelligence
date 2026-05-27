using NewsletterIntelligence.Domain.Entities;
using NewsletterIntelligence.Infrastructure.Services.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Services;

public class EmailNotionMapperService : IEmailNotionMapperService
{
    public Task<NotionPageDraft> MapEmail(IEnumerable<Email> emails)
    {
        throw new NotImplementedException();
    }
}