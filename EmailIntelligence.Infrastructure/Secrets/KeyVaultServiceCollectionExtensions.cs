using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EmailIntelligence.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace - conventional location for DI extensions.
namespace Microsoft.Extensions.DependencyInjection;

public static class KeyVaultServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure Key Vault secret access: options + validation, a single shared
    /// <see cref="SecretClient"/> (the recommended lifetime — it is thread-safe and pools
    /// connections), an in-memory cache, and <see cref="ISecretStore"/>. Authenticates
    /// with <see cref="DefaultAzureCredential"/> (managed identity in Azure, developer
    /// credentials locally).
    /// </summary>
    public static IServiceCollection AddKeyVaultSecrets(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KeyVaultOptions>()
            .Bind(configuration.GetSection(KeyVaultOptions.SectionName));
        services.AddSingleton<IValidateOptions<KeyVaultOptions>, KeyVaultOptionsValidator>();

        services.AddMemoryCache();

        services.AddSingleton(CreateSecretClient);
        services.AddSingleton<ISecretStore, KeyVaultSecretStore>();

        return services;
    }

    private static SecretClient CreateSecretClient(IServiceProvider serviceProvider)
    {
        // Resolving .Value here triggers KeyVaultOptionsValidator, so a misconfigured vault
        // fails fast on first use rather than on the first secret request.
        var options = serviceProvider.GetRequiredService<IOptions<KeyVaultOptions>>().Value;
        return new SecretClient(new Uri(options.VaultUri!), new DefaultAzureCredential());
    }
}
