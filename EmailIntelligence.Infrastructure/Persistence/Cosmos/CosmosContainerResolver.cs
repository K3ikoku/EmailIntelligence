using System.Collections.Concurrent;
using EmailIntelligence.Domain.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Persistence.Cosmos;

/// <summary>Resolves the <see cref="Container"/> handle for a given document type.</summary>
public interface ICosmosContainerResolver
{
    Container Resolve<T>() where T : IDocument;
    Container Resolve(Type documentType);
    IReadOnlyCollection<CosmosContainerRegistration> Registrations { get; }
}

/// <summary>
/// Caches <see cref="Container"/> handles (cheap, but worth reusing) keyed by document type,
/// using the registrations supplied via <c>AddCosmosContainer&lt;T&gt;</c>.
/// </summary>
public sealed class CosmosContainerResolver : ICosmosContainerResolver
{
    private readonly CosmosClient _client;
    private readonly string _databaseId;
    private readonly IReadOnlyDictionary<Type, CosmosContainerRegistration> _registrations;
    private readonly ConcurrentDictionary<Type, Container> _containers = new();

    public CosmosContainerResolver(
        CosmosClient client,
        IOptions<CosmosOptions> options,
        IEnumerable<CosmosContainerRegistration> registrations)
    {
        _client = client;
        _databaseId = options.Value.DatabaseId;
        _registrations = registrations.ToDictionary(r => r.DocumentType);
    }

    public IReadOnlyCollection<CosmosContainerRegistration> Registrations => _registrations.Values.ToList();

    public Container Resolve<T>() where T : IDocument => Resolve(typeof(T));

    public Container Resolve(Type documentType)
    {
        return _containers.GetOrAdd(documentType, type =>
        {
            if (!_registrations.TryGetValue(type, out var registration))
                throw new InvalidOperationException(
                    $"No Cosmos container registered for '{type.Name}'. " +
                    $"Call AddCosmosContainer<{type.Name}>(\"<container>\", \"/<partitionKeyPath>\").");

            return _client.GetContainer(_databaseId, registration.ContainerName);
        });
    }
}
