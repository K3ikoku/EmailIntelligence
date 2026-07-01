using EmailIntelligence.Infrastructure.Secrets;

namespace EmailIntelligence.Tests.Unit.Secrets;

public class KeyVaultOptionsValidatorTests
{
    private static bool Validate(KeyVaultOptions options, out string failures)
    {
        var result = new KeyVaultOptionsValidator().Validate(null, options);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Absolute_vault_uri_passes()
    {
        Validate(new KeyVaultOptions { VaultUri = "https://my-vault.vault.azure.net/" }, out _)
            .ShouldBeTrue();
    }

    [Fact]
    public void Missing_vault_uri_fails()
    {
        Validate(new KeyVaultOptions { VaultUri = "" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(KeyVaultOptions.VaultUri));
    }

    [Fact]
    public void Relative_vault_uri_fails()
    {
        Validate(new KeyVaultOptions { VaultUri = "my-vault" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(KeyVaultOptions.VaultUri));
    }

    [Fact]
    public void Non_positive_cache_duration_fails_when_caching_enabled()
    {
        Validate(
            new KeyVaultOptions { VaultUri = "https://my-vault.vault.azure.net/", CacheDurationSeconds = 0 },
            out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(KeyVaultOptions.CacheDurationSeconds));
    }

    [Fact]
    public void Non_positive_cache_duration_ignored_when_caching_disabled()
    {
        Validate(
            new KeyVaultOptions
            {
                VaultUri = "https://my-vault.vault.azure.net/",
                EnableCaching = false,
                CacheDurationSeconds = 0
            },
            out _).ShouldBeTrue();
    }
}
