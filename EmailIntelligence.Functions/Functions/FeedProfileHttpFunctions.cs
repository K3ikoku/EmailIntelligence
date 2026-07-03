using System.Net;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
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

public sealed class FeedProfileHttpFunctions(
    IFeedProfileService feedProfileService,
    ILogger<FeedProfileHttpFunctions> logger)
{
    [Function(nameof(CreateFeedProfile))]
    [OpenApiOperation(operationId: "CreateFeedProfile", tags: new[] { "Feed Profiles" },
        Summary = "Create a feed profile",
        Description = "Wires an input connector to an output connector and persists the profile.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateFeedProfileRequest),
        Required = true, Description = "The feed profile: its input/output connector ids, match/processing rules and front.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(FeedProfile), Summary = "The created feed profile.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> CreateFeedProfile(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/feed-profiles")] HttpRequest request)
    {
        logger.LogInformation("Create feed profile requested.");

        var contract = await ConfigurationHttp.DeserializeAsync<CreateFeedProfileRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var result = await feedProfileService.CreateFeedProfileAsync(
            contract.ToFeedProfile(), request.HttpContext.RequestAborted);

        return ConfigurationHttp.ToActionResult(result, logger);
    }
}
