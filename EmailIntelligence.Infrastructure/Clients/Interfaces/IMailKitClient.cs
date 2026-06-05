using MailKit;
using MimeKit;

namespace EmailIntelligence.Infrastructure.Clients.Interfaces;

public interface IMailKitClient
{
    Task<IEnumerable<MimeMessage>> GetEmails();
    Task<IEnumerable<UniqueId>> MoveToFolderAsync(IEnumerable<string> messageId);
}