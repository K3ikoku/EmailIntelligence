using NewsletterIntelligence.Infrastructure.Services.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Services;

public class NewsletterPipelineService(IEmailService emailService, IEmailNotionMapperService mapperService) : INewsletterPipelineService
{
    public async Task<bool> ProcessEmails()
    {
        var emails = await emailService.GetAndCleanEmails();
        var mappedEmails = mapperService.MapEmail(emails);
        return true;
    }
}