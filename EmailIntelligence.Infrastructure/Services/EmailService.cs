using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services.Interfaces;

namespace EmailIntelligence.Infrastructure.Services;

public class EmailService(IMailKitClient mailKitClient) : IEmailService
{
    public async Task<IEnumerable<Email>> GetAndCleanEmails()
    {
        var emails = await mailKitClient.GetEmails();
        var cleanedEmails = new List<Email>();
        foreach (var email in emails)
        {
            var senderName = email.From.First().Name;
            cleanedEmails.Add(new Email
            {
                EmailSender = senderName,
                Subject = email.Subject,
                DateReceived = email.Date,
                MessageId = email.MessageId,
                EmailBody = email.HtmlBody
            });
        }

        return cleanedEmails;
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