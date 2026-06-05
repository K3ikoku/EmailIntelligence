using System.Text.Json;
using Azure.Identity;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace - conventional location for DI extensions.
namespace Microsoft.Extensions.DependencyInjection;

public static class CosmosServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cosmos DB persistence: options + validation, a single shared
    /// <see cref="CosmosClient"/> (the recommended lifetime), the container resolver, the
    /// open-generic <see cref="IRepository{T}"/>, and a startup initializer. Register the
    /// containers your documents use with <see cref="AddCosmosContainer{T}"/>.
    /// </summary>
    public static IServiceCollection AddCosmosPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CosmosOptions>()
            .Bind(configuration.GetSection(CosmosOptions.SectionName));
        services.AddSingleton<IValidateOptions<CosmosOptions>, CosmosOptionsValidator>();

        // One CosmosClient per app: it is thread-safe, pools connections, and is expensive
        // to create. A singleton is the single most important Cosmos best practice.
        services.AddSingleton(CreateCosmosClient);
        services.AddSingleton<ICosmosContainerResolver, CosmosContainerResolver>();
        services.AddSingleton(typeof(IRepository<>), typeof(CosmosRepository<>));
        services.AddHostedService<CosmosInitializer>();

        return services;
    }

    /// <summary>Maps a document type to its container and partition-key path (e.g. <c>"/sender"</c>).</summary>
    public static IServiceCollection AddCosmosContainer<T>(
        this IServiceCollection services, string containerName, string partitionKeyPath)
        where T : IDocument
    {
        services.AddSingleton(new CosmosContainerRegistration(typeof(T), containerName, partitionKeyPath));
        return services;
    }

    private static CosmosClient CreateCosmosClient(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<CosmosOptions>>().Value;

        var clientOptions = new CosmosClientOptions
        {
            ApplicationName = options.ApplicationName,
            ConnectionMode = options.ConnectionMode,
            MaxRetryAttemptsOnRateLimitedRequests = options.MaxRetryAttemptsOnRateLimited,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(options.MaxRetryWaitTimeSeconds),
            AllowBulkExecution = options.AllowBulkExecution,
            // System.Text.Json (web defaults => camelCase) instead of the SDK's Newtonsoft default.
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        };

        // Prefer Entra ID (managed identity / DefaultAzureCredential); fall back to a
        // connection string for local development and the Cosmos emulator.
        return string.IsNullOrWhiteSpace(options.ConnectionString)
            ? new CosmosClient(options.AccountEndpoint, new DefaultAzureCredential(), clientOptions)
            : new CosmosClient(options.ConnectionString, clientOptions);
    }
}
