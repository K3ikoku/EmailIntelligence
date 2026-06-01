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
        var notionPageDrafts = emails.Select(mapperService.MapEmail).ToList();
        var createdPages = notionPageDrafts.Select(notionClient.CreatePage).ToList();

        return true;
    }
}