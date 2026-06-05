using EmailIntelligence.Domain.Persistence;

namespace EmailIntelligence.Infrastructure.Persistence.Cosmos;

/// <summary>
/// Maps a document type to its Cosmos container and partition-key path. Registered once
/// per document type via <c>AddCosmosContainer&lt;T&gt;</c> and consumed by the resolver
/// and the startup initializer.
/// </summary>
/// <param name="DocumentType">The <see cref="IDocument"/> CLR type.</param>
/// <param name="ContainerName">The Cosmos container id.</param>
/// <param name="PartitionKeyPath">The partition-key path, e.g. <c>/sender</c>. Must match a serialized field.</param>
public sealed record CosmosContainerRegistration(Type DocumentType, string ContainerName, string PartitionKeyPath);
