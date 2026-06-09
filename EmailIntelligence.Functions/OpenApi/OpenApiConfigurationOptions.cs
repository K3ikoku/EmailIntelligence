using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace EmailIntelligence.Functions.OpenApi;

/// <summary>
/// Document metadata for the generated OpenAPI spec and Swagger UI. Registered as
/// <see cref="IOpenApiConfigurationOptions"/> in <c>Program.cs</c>; forces OpenAPI v3.
/// </summary>
public sealed class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new()
    {
        Title = "EmailIntelligence API",
        Version = "1.0.0",
        Description = "Runs the email → Notion newsletter pipeline on demand or on a schedule."
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
