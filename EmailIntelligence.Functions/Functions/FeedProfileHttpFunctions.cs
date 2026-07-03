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
    [Function(nameof(UpsertFeedProfile))]
    [OpenApiOperation(operationId: "UpsertFeedProfile", tags: ["Feed Profiles"],
        Summary = "Create or update a feed profile",
        Description = "Wires an input connector to an output connector and persists the profile. "
                      + "Pass an existing id in the body to update, or omit it to create.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateFeedProfileRequest),
        Required = true,
        Description = "The feed profile: its input/output connector ids, match/processing rules and front.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(FeedProfile), Summary = "The saved feed profile.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> UpsertFeedProfile(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/feed-profiles")]
        HttpRequest request)
    {
        logger.LogInformation("Upsert feed profile requested.");

        var contract = await ConfigurationHttp.DeserializeAsync<CreateFeedProfileRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var result = await feedProfileService.UpsertFeedProfileAsync(
            contract.ToFeedProfile(), request.HttpContext.RequestAborted);

        return ConfigurationHttp.ToActionResult(result, logger);
    }

    [Function(nameof(DeleteFeedProfile))]
    [OpenApiOperation(operationId: "DeleteFeedProfile", tags: ["Feed Profiles"],
        Summary = "Delete a feed profile",
        Description = "Deletes the feed profile with the given id.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string),
        Description = "The feed profile id.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "The feed profile was deleted.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "No feed profile with that id exists.")]
    public async Task<IActionResult> DeleteFeedProfile(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "configurations/feed-profiles/{id}")]
        HttpRequest request,
        string id)
    {
        logger.LogInformation("Delete feed profile {FeedProfileId} requested.", id);

        var deleted = await feedProfileService.DeleteFeedProfileAsync(id, request.HttpContext.RequestAborted);

        return deleted ? new NoContentResult() : new NotFoundResult();
    }
}
