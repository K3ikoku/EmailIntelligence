using NewsletterIntelligence.Domain.Entities;
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
        var emails = await emailService.GetAndCleanEmails();
        var notionPageDrafts = new List<NotionPageDraft>();
        foreach (var email in emails)
            notionPageDrafts.Add(await mapperService.MapEmail(email));

        var createdPages = new List<string>();
        foreach (var draft in notionPageDrafts)
            createdPages.Add(await notionClient.CreatePage(draft));

        return true;
    }
}