using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using MimeKit;

namespace EmailIntelligence.Infrastructure.Services;

public class EmailService(IMailKitClient mailKitClient) : IEmailService
{
    public async Task<IEnumerable<Email>> GetAndCleanEmails()
    {
        var emails = await mailKitClient.GetEmails();
        var cleanedEmails = new List<Email>();
        foreach (var email in emails)
        {
            cleanedEmails.Add(new Email
            {
                EmailSender = ResolveSender(email),
                Subject = email.Subject ?? string.Empty,
                DateReceived = email.Date,
                MessageId = email.MessageId ?? string.Empty,
                // HtmlBody is null for plain-text-only messages; the extractor treats
                // an empty body as "no blocks" rather than throwing.
                EmailBody = email.HtmlBody ?? string.Empty
            });
        }

        return cleanedEmails;
    }

    private static string ResolveSender(MimeMessage email)
    {
        var mailbox = email.From.Mailboxes.FirstOrDefault();
        if (mailbox is null)
            return "Unknown sender";

        return !string.IsNullOrWhiteSpace(mailbox.Name) ? mailbox.Name : mailbox.Address;
    }

    public async Task MoveProcessedEmailsAsync(IEnumerable<string> messageIds)
    {
        var ids = await mailKitClient.MoveToFolderAsync(messageIds);
        if (!ids.Any())
        {
            throw new Exception("Failed to move email to processed folder");
        }
    }
}