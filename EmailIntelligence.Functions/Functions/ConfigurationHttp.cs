using System.Text.Json;
using EmailIntelligence.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Functions.Functions;

internal static class ConfigurationHttp
{
    public static async Task<T?> DeserializeAsync<T>(HttpRequest request) where T : class
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

    public static IActionResult ToActionResult<T>(ConfigurationResult<T> result, ILogger logger)
    {
        if (result.Succeeded)
            return new OkObjectResult(result.Value);

        logger.LogInformation("Configuration create rejected: {Errors}", string.Join("; ", result.Errors));
        return new BadRequestObjectResult(result.Errors);
    }
}
