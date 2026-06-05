using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Binds the <c>Cosmos</c> configuration section. Prefer Entra ID (managed identity) in
/// real environments by setting only <see cref="AccountEndpoint"/>; use
/// <see cref="ConnectionString"/> for local development / the Cosmos emulator.
/// </summary>
public sealed record CosmosOptions
{
    public const string SectionName = "Cosmos";

    /// <summary>Account URI, e.g. <c>https://my-account.documents.azure.com:443/</c>. Used with managed identity.</summary>
    public string? AccountEndpoint { get; init; }

    /// <summary>Full connection string (account key). Local/dev only — leave unset in production.</summary>
    public string? ConnectionString { get; init; }

    public required string DatabaseId { get; init; }

    /// <summary>Surfaces in Cosmos metrics/diagnostics to identify the calling app.</summary>
    public string ApplicationName { get; init; } = "EmailIntelligence";

    /// <summary><see cref="ConnectionMode.Direct"/> is fastest; use Gateway behind restrictive firewalls/consumption plans.</summary>
    public ConnectionMode ConnectionMode { get; init; } = ConnectionMode.Direct;

    public int MaxRetryAttemptsOnRateLimited { get; init; } = 9;

    public int MaxRetryWaitTimeSeconds { get; init; } = 30;

    /// <summary>Enable for high-volume parallel writes (e.g. <c>CreateManyAsync</c>); adds latency to single ops.</summary>
    public bool AllowBulkExecution { get; init; }

    /// <summary>Dev convenience: create the database/containers at startup. Use IaC (Bicep) in production.</summary>
    public bool CreateResourcesOnStartup { get; init; } = true;

    /// <summary>Optional shared database throughput (RU/s). <c>null</c> = serverless or per-container throughput.</summary>
    public int? DatabaseThroughput { get; init; }
}

public sealed class CosmosOptionsValidator : IValidateOptions<CosmosOptions>
{
    public ValidateOptionsResult Validate(string? name, CosmosOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DatabaseId))
            failures.Add($"{nameof(CosmosOptions.DatabaseId)} is required.");

        if (string.IsNullOrWhiteSpace(options.AccountEndpoint) && string.IsNullOrWhiteSpace(options.ConnectionString))
            failures.Add($"Either {nameof(CosmosOptions.AccountEndpoint)} (managed identity) or {nameof(CosmosOptions.ConnectionString)} (local) must be set.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
