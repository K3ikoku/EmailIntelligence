namespace EmailIntelligence.Infrastructure.Services.Interfaces;

public interface INewsletterPipelineService
{
    Task<bool> ProcessEmails();
}