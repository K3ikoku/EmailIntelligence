using System.Net;
using EmailIntelligence.Functions.Contracts;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace EmailIntelligence.Functions.Functions;

public sealed class NewsletterHttpFunction(
    INewsletterPipelineService pipeline,
    ILogger<NewsletterHttpFunction> logger)
{
    [Function(nameof(NewsletterHttpFunction))]
    [OpenApiOperation(operationId: "RunNewsletterPipeline", tags: ["Newsletter"],
        Summary = "Run the newsletter pipeline",
        Description = "Fetches unread newsletter emails over IMAP and creates the matching Notion pages, "
                      + "then returns whether the run completed successfully.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(PipelineRunResponse),
        Summary = "Pipeline result", Description = "Whether the pipeline run completed successfully.")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "newsletter/run")] HttpRequest request)
    {
        logger.LogInformation("Newsletter pipeline (HTTP) triggered.");

        var success = await pipeline.ProcessEmails();

        return new OkObjectResult(new PipelineRunResponse { Success = success });
    }
}
