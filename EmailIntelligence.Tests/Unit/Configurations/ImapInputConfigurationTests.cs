using System.Text.RegularExpressions;
using EmailIntelligence.Domain.Entities.Configurations;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class ImapInputConfigurationTests
{
    // Azure Key Vault secret names may only contain [0-9a-zA-Z-] and be 1-127 chars long.
    private static readonly Regex KeyVaultSecretName = new("^[0-9a-zA-Z-]{1,127}$");

    private static ImapInputConfiguration Config(string username) => new()
    {
        Host = "imap.example.com",
        Port = 993,
        Username = username,
        UseSsl = true,
        RetrievingFolder = "INBOX",
        ProcessedFolder = "Processed"
    };

    [Theory]
    [InlineData("user.name@example.com")]    // email: '.' and '@' are illegal in a KV name
    [InlineData("björn.åström@example.com")] // non-ASCII letters are illegal too
    [InlineData("plain-user")]
    [InlineData("user_name 123")]
    public void ImapPasswordId_is_a_valid_key_vault_secret_name(string username)
    {
        Config(username).ImapPasswordId.ShouldMatch(KeyVaultSecretName.ToString());
    }

    [Fact]
    public void ImapPasswordId_is_deterministic_for_the_same_username()
    {
        Config("user@example.com").ImapPasswordId
            .ShouldBe(Config("user@example.com").ImapPasswordId);
    }

    [Fact]
    public void ImapPasswordId_is_derived_from_the_username()
    {
        Config("alice@example.com").ImapPasswordId.ShouldBe("imap-alice-example-com-password");
    }
}
