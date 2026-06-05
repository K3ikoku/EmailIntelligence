using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Creates the database and the registered containers at startup when
/// <see cref="CosmosOptions.CreateResourcesOnStartup"/> is enabled. This is a development
/// convenience — in production, provision Cosmos resources via infrastructure-as-code
/// (Bicep/Terraform) and set the flag to <c>false</c>.
/// </summary>
public sealed class CosmosInitializer(
    CosmosClient client,
    ICosmosContainerResolver resolver,
    IOptions<CosmosOptions> options,
    ILogger<CosmosInitializer> logger) : IHostedService
{
    private readonly CosmosOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.CreateResourcesOnStartup)
            return;

        logger.LogInformation("Ensuring Cosmos database '{Database}' and containers exist.", _options.DatabaseId);

        var database = await client.CreateDatabaseIfNotExistsAsync(
            _options.DatabaseId, throughput: _options.DatabaseThroughput, cancellationToken: cancellationToken);

        foreach (var registration in resolver.Registrations)
        {
            await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(registration.ContainerName, registration.PartitionKeyPath),
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Container '{Container}' ready (partition key '{PartitionKeyPath}').",
                registration.ContainerName, registration.PartitionKeyPath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
