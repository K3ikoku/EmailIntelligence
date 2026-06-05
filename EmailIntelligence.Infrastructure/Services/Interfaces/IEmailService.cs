using EmailIntelligence.Domain.Entities;

namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface IEmailService
{
    Task<IEnumerable<Email>> GetAndCleanEmails();
    Task MoveProcessedEmailsAsync(IEnumerable<string> messageIds);
}