using NewsletterIntelligence.Domain.Entities;

namespace NewsletterIntelligence.Infrastructure.Services.Interfaces;

public interface IEmailNotionMapperService
{
    NotionPageDraft MapEmail(Email email);
}