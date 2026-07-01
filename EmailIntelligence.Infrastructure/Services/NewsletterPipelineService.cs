using System.Diagnostics;
using System.Globalization;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Infrastructure.Services;

public class NewsletterPipelineService(
    IEmailService emailService,
    IEmailNotionMapperService mapperService,
    INotionService notionService,
    TelemetryClient telemetryClient,
    ILogger<NewsletterPipelineService> logger) : INewsletterPipelineService
{
    private const string RunCompletedEvent = "NewsletterRunCompleted";

    public async Task<bool> ProcessEmails()
    {
        // Correlate every log/metric/exception emitted during this run.
        var runId = Guid.NewGuid().ToString("N");
        using var _ = logger.BeginScope(new Dictionary<string, object> { ["RunId"] = runId });
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Newsletter pipeline run {RunId} started.", runId);

        try
        {
            var emails = (await emailService.GetAndCleanEmails()).ToList();
            logger.LogInformation("Fetched {EmailsFetched} email(s) from IMAP.", emails.Count);

            var notionPageDrafts = emails.Select(mapperService.MapEmail).ToList();

            var processedIds = (await notionService.CreatePage(notionPageDrafts)).ToList();
            logger.LogInformation("Processed {PagesProcessed} Notion page(s).", processedIds.Count);

            if (processedIds.Count > 0)
            {
                await emailService.MoveProcessedEmailsAsync(processedIds);
                logger.LogInformation("Moved {MovedCount} processed email(s) to the processed folder.",
                    processedIds.Count);
            }

            stopwatch.Stop();

            // Pre-aggregated metrics for charts/alerts in Application Insights.
            telemetryClient.GetMetric("Newsletter.EmailsFetched").TrackValue(emails.Count);
            telemetryClient.GetMetric("Newsletter.PagesProcessed").TrackValue(processedIds.Count);
            telemetryClient.GetMetric("Newsletter.RunDurationMs").TrackValue(stopwatch.Elapsed.TotalMilliseconds);

            // Custom event carrying the full per-run summary (queryable via customDimensions).
            TrackRunCompleted("Success", runId, stopwatch.Elapsed, emails.Count, processedIds.Count);

            logger.LogInformation(
                "Newsletter pipeline run {RunId} completed in {DurationMs:F0} ms (fetched {EmailsFetched}, processed {PagesProcessed}).",
                runId, stopwatch.Elapsed.TotalMilliseconds, emails.Count, processedIds.Count);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var failure = new ExceptionTelemetry(ex);
            failure.Properties["RunId"] = runId;
            failure.Properties["Outcome"] = "Failed";
            telemetryClient.TrackException(failure);

            TrackRunCompleted("Failed", runId, stopwatch.Elapsed, emailsFetched: null, pagesProcessed: null);

            logger.LogError(ex,
                "Newsletter pipeline run {RunId} failed after {DurationMs:F0} ms.",
                runId, stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
    }

    private void TrackRunCompleted(
        string outcome, string runId, TimeSpan duration, int? emailsFetched, int? pagesProcessed)
    {
        var completed = new EventTelemetry(RunCompletedEvent);
        completed.Properties["RunId"] = runId;
        completed.Properties["Outcome"] = outcome;
        completed.Properties["DurationMs"] = duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture);

        if (emailsFetched is { } fetched)
            completed.Properties["EmailsFetched"] = fetched.ToString(CultureInfo.InvariantCulture);
        if (pagesProcessed is { } processed)
            completed.Properties["PagesProcessed"] = processed.ToString(CultureInfo.InvariantCulture);

        telemetryClient.TrackEvent(completed);
    }
}
