using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Functions.Functions;

/// <summary>
/// Runs the email → Notion pipeline on demand: <c>POST /api/newsletter/run</c>.
/// Protected by a function key (<see cref="AuthorizationLevel.Function"/>).
/// </summary>
public sealed class NewsletterHttpFunction(
    INewsletterPipelineService pipeline,
    ILogger<NewsletterHttpFunction> logger)
{
    [Function(nameof(NewsletterHttpFunction))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "newsletter/run")] HttpRequest request)
    {
        logger.LogInformation("Newsletter pipeline (HTTP) triggered.");

        var success = await pipeline.ProcessEmails();

        return new OkObjectResult(new { success });
    }
}
