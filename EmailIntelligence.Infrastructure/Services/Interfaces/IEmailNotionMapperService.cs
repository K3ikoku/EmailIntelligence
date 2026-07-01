using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Entities.Drafts.Notion;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IEmailNotionMapperService
{
    Page MapEmail(Email email);
}