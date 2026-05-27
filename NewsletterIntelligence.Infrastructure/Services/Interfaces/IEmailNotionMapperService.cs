using NewsletterIntelligence.Domain.Entities;

namespace NewsletterIntelligence.Infrastructure.Services.Interfaces;

public interface IEmailNotionMapperService
{
    Task<NotionPageDraft> MapEmail(Email email);
}