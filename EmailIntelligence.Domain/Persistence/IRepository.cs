using System.Linq.Expressions;

namespace EmailIntelligence.Domain.Persistence;

/// <summary>
/// Generic repository over a document collection. Implementations are expected to be
/// safe to register as singletons. All reads require the partition key where known, to
/// keep operations single-partition (the cheapest, most scalable access pattern).
/// </summary>
public interface IRepository<T> where T : IDocument
{
    /// <summary>Point-read by id + partition key. Returns <c>null</c> if not found.</summary>
    Task<T?> GetAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Creates a new item; throws if an item with the same id already exists.</summary>
    Task<T> CreateAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>Creates many items concurrently (uses bulk execution when enabled).</summary>
    Task CreateManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);

    /// <summary>Inserts or replaces the item regardless of its current state.</summary>
    Task<T> UpsertAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing item. When <see cref="IDocument.ETag"/> is set the write uses
    /// optimistic concurrency and fails if the stored item changed in the meantime.
    /// </summary>
    Task<T> ReplaceAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>Deletes by id + partition key. No-op if the item does not exist.</summary>
    Task DeleteAsync(string id, string partitionKey, CancellationToken cancellationToken = default);

    /// <summary>Materializes all items matching <paramref name="predicate"/>. Use for small result sets.</summary>
    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Returns a single page plus a continuation token for fetching the next page.</summary>
    Task<PagedResult<T>> GetPageAsync(
        Expression<Func<T, bool>> predicate,
        int pageSize,
        string? continuationToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>Streams matching items lazily, transparently fetching pages as needed.</summary>
    IAsyncEnumerable<T> StreamAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}

/// <summary>One page of results and the token to resume after it (<c>null</c> when exhausted).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? ContinuationToken);
