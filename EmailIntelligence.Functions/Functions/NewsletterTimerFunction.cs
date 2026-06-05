using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Functions.Functions;

/// <summary>
/// Runs the email → Notion pipeline on a schedule. The CRON expression comes from the
/// <c>NewsletterSchedule</c> app setting (see local.settings.json), defaulting in docs to
/// every 6 hours: <c>0 0 */6 * * *</c>.
/// </summary>
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
