using System.Net;
using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
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

public sealed class ConfigurationHttpFunctions(
    IConfigurationService configurationService,
    ILogger<ConfigurationHttpFunctions> logger)
{
    [Function(nameof(UpsertImapInputConfiguration))]
    [OpenApiOperation(operationId: "UpsertImapInputConfiguration", tags: ["Configurations"],
        Summary = "Create or update an IMAP input configuration",
        Description = "Stores the account password in Key Vault and persists the configuration. "
                      + "Pass an existing id in the body to update, or omit it to create.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateImapInputConfigurationRequest),
        Required = true, Description = "The IMAP account details. The password is written to Key Vault, not persisted.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(ImapInputConfiguration), Summary = "The saved configuration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> UpsertImapInputConfiguration(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/imap")] HttpRequest request)
    {
        logger.LogInformation("Upsert IMAP input configuration requested.");

        var contract = await ConfigurationHttp.DeserializeAsync<CreateImapInputConfigurationRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var secretErrors = contract.ValidateSecret();
        if (secretErrors.Count > 0)
            return new BadRequestObjectResult(secretErrors);

        var result = await configurationService.UpsertImapInputConfigurationAsync(
            contract.ToConfiguration(), contract.Password ?? string.Empty, request.HttpContext.RequestAborted);

        return ConfigurationHttp.ToActionResult(result, logger);
    }

    [Function(nameof(UpsertNotionOutputConfiguration))]
    [OpenApiOperation(operationId: "UpsertNotionOutputConfiguration", tags: ["Configurations"],
        Summary = "Create or update a Notion output configuration",
        Description = "Stores the auth token in Key Vault and persists the configuration. "
                      + "Pass an existing id in the body to update, or omit it to create.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateNotionOutputConfigurationRequest),
        Required = true, Description = "The Notion auth token and target pages. The token is written to Key Vault, not persisted.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(NotionOutputConfiguration), Summary = "The saved configuration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> UpsertNotionOutputConfiguration(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/notion")] HttpRequest request)
    {
        logger.LogInformation("Upsert Notion output configuration requested.");

        var contract = await ConfigurationHttp.DeserializeAsync<CreateNotionOutputConfigurationRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var secretErrors = contract.ValidateSecret();
        if (secretErrors.Count > 0)
            return new BadRequestObjectResult(secretErrors);

        var result = await configurationService.UpsertNotionOutputConfigurationAsync(
            contract.ToConfiguration(), contract.AuthToken ?? string.Empty, request.HttpContext.RequestAborted);

        return ConfigurationHttp.ToActionResult(result, logger);
    }

    [Function(nameof(DeleteConnector))]
    [OpenApiOperation(operationId: "DeleteConnector", tags: ["Configurations"],
        Summary = "Delete a connector configuration",
        Description = "Deletes the connector configuration and its associated Key Vault secret, whatever its type.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string),
        Description = "The connector configuration id.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "The connector was deleted.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "No connector with that id exists.")]
    public async Task<IActionResult> DeleteConnector(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "configurations/connectors/{id}")] HttpRequest request,
        string id)
    {
        logger.LogInformation("Delete connector {ConnectorId} requested.", id);

        var deleted = await configurationService.DeleteConnectorAsync(id, request.HttpContext.RequestAborted);

        return deleted ? new NoContentResult() : new NotFoundResult();
    }
}
