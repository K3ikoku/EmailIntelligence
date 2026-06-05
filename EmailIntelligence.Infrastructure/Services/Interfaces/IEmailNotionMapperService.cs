using EmailIntelligence.Domain.Entities;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IEmailNotionMapperService
{
    NotionPageDraft MapEmail(Email email);
}