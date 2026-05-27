using NewsletterIntelligence.Domain.Entities;
using NewsletterIntelligence.Infrastructure.Clients.Interfaces;
using NewsletterIntelligence.Infrastructure.Services.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Services;

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
                EmailUuid = email.MessageId,
                EmailBody = email.HtmlBody
            });
        }

        return cleanedEmails;
    }
}