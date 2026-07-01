using System.Net;
using System.Text.Json;
using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Functions.Contracts;
using EmailIntelligence.Infrastructure.Services;
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
    [Function(nameof(CreateImapInputConfiguration))]
    [OpenApiOperation(operationId: "CreateImapInputConfiguration", tags: new[] { "Configurations" },
        Summary = "Create an IMAP input configuration",
        Description = "Stores the account password in Key Vault and returns the built configuration.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateImapInputConfigurationRequest),
        Required = true, Description = "The IMAP account details. The password is written to Key Vault, not persisted.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(ImapInputConfiguration), Summary = "The created configuration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> CreateImapInputConfiguration(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/imap")] HttpRequest request)
    {
        logger.LogInformation("Create IMAP input configuration requested.");

        var contract = await DeserializeAsync<CreateImapInputConfigurationRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var secretErrors = contract.ValidateSecret();
        if (secretErrors.Count > 0)
            return new BadRequestObjectResult(secretErrors);

        var result = await configurationService.CreateImapInputConfigurationAsync(
            contract.ToConfiguration(), contract.Password ?? string.Empty, request.HttpContext.RequestAborted);

        return ToActionResult(result);
    }

    [Function(nameof(CreateNotionOutputConfiguration))]
    [OpenApiOperation(operationId: "CreateNotionOutputConfiguration", tags: new[] { "Configurations" },
        Summary = "Create a Notion output configuration",
        Description = "Stores the auth token in Key Vault under a server-generated id and returns the built configuration.")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey,
        Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateNotionOutputConfigurationRequest),
        Required = true, Description = "The Notion auth token and target pages. The token is written to Key Vault, not persisted.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json",
        bodyType: typeof(NotionOutputConfiguration), Summary = "The created configuration.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json",
        bodyType: typeof(IEnumerable<string>), Summary = "Validation errors.")]
    public async Task<IActionResult> CreateNotionOutputConfiguration(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "configurations/notion")] HttpRequest request)
    {
        logger.LogInformation("Create Notion output configuration requested.");

        var contract = await DeserializeAsync<CreateNotionOutputConfigurationRequest>(request);
        if (contract is null)
            return new BadRequestObjectResult(new[] { "A JSON request body is required." });

        var secretErrors = contract.ValidateSecret();
        if (secretErrors.Count > 0)
            return new BadRequestObjectResult(secretErrors);

        var result = await configurationService.CreateNotionOutputConfigurationAsync(
            contract.ToConfiguration(), contract.AuthToken ?? string.Empty, request.HttpContext.RequestAborted);

        return ToActionResult(result);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpRequest request) where T : class
    {
        try
        {
            return await request.ReadFromJsonAsync<T>();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private IActionResult ToActionResult<T>(ConfigurationResult<T> result)
    {
        if (result.Succeeded)
            return new OkObjectResult(result.Value);

        logger.LogInformation("Configuration create rejected: {Errors}", string.Join("; ", result.Errors));
        return new BadRequestObjectResult(result.Errors);
    }
}
