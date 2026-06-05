using EmailIntelligence.Infrastructure.Services.Interfaces;

namespace EmailIntelligence.Infrastructure.Services;

public class NewsletterPipelineService(
    IEmailService emailService,
    IEmailNotionMapperService mapperService,
    INotionService notionService) : INewsletterPipelineService
{
    public async Task<bool> ProcessEmails()
    {
        var emails = (await emailService.GetAndCleanEmails()).ToList();
        var notionPageDrafts = emails.Select(mapperService.MapEmail).ToList();
        var createdIds = await notionService.CreatePage(notionPageDrafts);
        await emailService.MoveProcessedEmailsAsync(createdIds);

        return true;
    }
}