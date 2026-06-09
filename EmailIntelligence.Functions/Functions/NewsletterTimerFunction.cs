using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Functions.Functions;

public sealed class NewsletterTimerFunction(
    INewsletterPipelineService pipeline,
    ILogger<NewsletterTimerFunction> logger)
{
    [Function(nameof(NewsletterTimerFunction))]
    public async Task Run([TimerTrigger("%NewsletterSchedule%")] TimerInfo timer)
    {
        logger.LogInformation("Newsletter pipeline (timer) started at {Start:o}.", DateTimeOffset.UtcNow);

        var success = await pipeline.ProcessEmails();

        logger.LogInformation(
            "Newsletter pipeline (timer) finished (success={Success}). Next scheduled run: {Next:o}.",
            success, timer.ScheduleStatus?.Next);
    }
}
