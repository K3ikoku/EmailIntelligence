using Azure.Identity;
// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.Configuration;

public static class KeyVaultConfigurationExtensions
{
    public static IConfigurationManager AddKeyVaultConfiguration(this IConfigurationManager configuration)
    {
        var vaultUri = configuration["KeyVault:VaultUri"];
        if (!string.IsNullOrWhiteSpace(vaultUri) && Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri))
        {
            configuration.AddAzureKeyVault(uri, new DefaultAzureCredential());
        }

        return configuration;
    }
}
