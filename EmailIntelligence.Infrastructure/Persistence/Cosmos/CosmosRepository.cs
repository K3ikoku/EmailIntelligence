using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using EmailIntelligence.Domain.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace EmailIntelligence.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Cosmos DB implementation of <see cref="IRepository{T}"/>. Safe to use as a singleton;
/// it holds only a cached <see cref="Container"/> handle obtained from the resolver.
/// </summary>
public sealed class CosmosRepository<T>(ICosmosContainerResolver resolver) : IRepository<T>
    where T : IDocument
{
    // The LINQ provider must translate member names with the same camelCase policy as the
    // System.Text.Json serializer, otherwise predicates reference the wrong JSON paths.
    private static readonly CosmosLinqSerializerOptions LinqOptions =
        new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase };

    private readonly Container _container = resolver.Resolve<T>();

    public async Task<T?> GetAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(
                id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async Task<T> CreateAsync(T item, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            item, new PartitionKey(item.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public Task CreateManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        // With CosmosOptions.AllowBulkExecution = true the SDK coalesces these concurrent
        // operations into efficient bulk batches per partition.
        var tasks = items.Select(item =>
            _container.CreateItemAsync(item, new PartitionKey(item.PartitionKey), cancellationToken: cancellationToken));
        return Task.WhenAll(tasks);
    }

    public async Task<T> UpsertAsync(T item, CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(
            item, new PartitionKey(item.PartitionKey), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default)
    {
        var options = string.IsNullOrEmpty(item.ETag)
            ? null
            : new ItemRequestOptions { IfMatchEtag = item.ETag };

        var response = await _container.ReplaceItemAsync(
            item, item.Id, new PartitionKey(item.PartitionKey), options, cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Idempotent delete.
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var iterator = _container
            .GetItemLinqQueryable<T>(linqSerializerOptions: LinqOptions)
            .Where(predicate)
            .ToFeedIterator();

        var results = new List<T>();
        while (iterator.HasMoreResults)
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));

        return results;
    }

    public async Task<PagedResult<T>> GetPageAsync(
        Expression<Func<T, bool>> predicate,
        int pageSize,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        using var iterator = _container
            .GetItemLinqQueryable<T>(
                continuationToken: continuationToken,
                requestOptions: new QueryRequestOptions { MaxItemCount = pageSize },
                linqSerializerOptions: LinqOptions)
            .Where(predicate)
            .ToFeedIterator();

        if (!iterator.HasMoreResults)
            return new PagedResult<T>([], null);

        var response = await iterator.ReadNextAsync(cancellationToken);
        return new PagedResult<T>(response.ToList(), response.ContinuationToken);
    }

    public async IAsyncEnumerable<T> StreamAsync(
        Expression<Func<T, bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var iterator = _container
            .GetItemLinqQueryable<T>(linqSerializerOptions: LinqOptions)
            .Where(predicate)
            .ToFeedIterator();

        while (iterator.HasMoreResults)
            foreach (var item in await iterator.ReadNextAsync(cancellationToken))
                yield return item;
    }
}
