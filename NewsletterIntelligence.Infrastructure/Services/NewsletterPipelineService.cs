using NewsletterIntelligence.Infrastructure.Clients;
using NewsletterIntelligence.Infrastructure.Services.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Services;

public class NewsletterPipelineService(
    IEmailService emailService,
    IEmailNotionMapperService mapperService,
    NotionApiClient notionClient) : INewsletterPipelineService
{
    public async Task<bool> ProcessEmails()
    {
        var emails = (await emailService.GetAndCleanEmails()).ToList();
        var notionPageDrafts = emails.Select(mapperService.MapEmail).ToList();
        // await Task.WhenAll(notionPageDrafts.Select(notionClient.CreatePage));
        await emailService.MoveProcessedEmailsAsync(emails.Select(e => e.MessageId));

        return true;
    }
}