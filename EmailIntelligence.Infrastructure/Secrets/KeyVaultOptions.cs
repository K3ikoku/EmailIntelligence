using Microsoft.Extensions.Options;

namespace EmailIntelligence.Infrastructure.Secrets;

public sealed record KeyVaultOptions
{
    public const string SectionName = "KeyVault";
    public string? VaultUri { get; init; }
    public bool EnableCaching { get; init; } = true;
    public int CacheDurationSeconds { get; init; } = 300;
}

public sealed class KeyVaultOptionsValidator : IValidateOptions<KeyVaultOptions>
{
    public ValidateOptionsResult Validate(string? name, KeyVaultOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.VaultUri))
            failures.Add($"{nameof(KeyVaultOptions.VaultUri)} is required.");
        else if (!Uri.TryCreate(options.VaultUri, UriKind.Absolute, out _))
            failures.Add($"{nameof(KeyVaultOptions.VaultUri)} must be an absolute URI (e.g. https://my-vault.vault.azure.net/).");

        if (options.EnableCaching && options.CacheDurationSeconds <= 0)
            failures.Add($"{nameof(KeyVaultOptions.CacheDurationSeconds)} must be greater than zero when caching is enabled.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
