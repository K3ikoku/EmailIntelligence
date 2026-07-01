using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Secrets;

public sealed class KeyVaultSecretStore(
    SecretClient client,
    IMemoryCache cache,
    IOptions<KeyVaultOptions> options,
    ILogger<KeyVaultSecretStore> logger) : ISecretStore
{
    private const string CacheKeyPrefix = "kv-secret:";
    private readonly KeyVaultOptions _options = options.Value;

    public async Task<string> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = await TryGetSecretAsync(name, cancellationToken);
        return value ?? throw new KeyNotFoundException($"Secret '{name}' was not found in Key Vault.");
    }

    public async Task<string?> TryGetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var cacheKey = CacheKeyPrefix + name;
        if (_options.EnableCaching && cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        try
        {
            KeyVaultSecret secret = await client.GetSecretAsync(name, cancellationToken: cancellationToken);
            Cache(cacheKey, secret.Value);
            return secret.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Secret '{SecretName}' was not found in Key Vault.", name);
            return null;
        }
    }

    public async Task<string> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        KeyVaultSecret secret = await client.SetSecretAsync(name, value, cancellationToken);
        Cache(CacheKeyPrefix + name, secret.Value);

        logger.LogInformation("Secret '{SecretName}' written to Key Vault.", name);
        return secret.Value;
    }

    public async Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        cache.Remove(CacheKeyPrefix + name);

        try
        {
            await client.StartDeleteSecretAsync(name, cancellationToken);
            logger.LogInformation("Secret '{SecretName}' deletion started in Key Vault.", name);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning("Secret '{SecretName}' was not found; nothing to delete.", name);
        }
    }

    private void Cache(string cacheKey, string value)
    {
        if (_options.EnableCaching)
            cache.Set(cacheKey, value, TimeSpan.FromSeconds(_options.CacheDurationSeconds));
    }
}
