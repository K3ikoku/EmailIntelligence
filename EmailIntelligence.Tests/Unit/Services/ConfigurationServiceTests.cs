using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Infrastructure.Secrets;
using EmailIntelligence.Infrastructure.Services;

namespace EmailIntelligence.Tests.Unit.Services;

public class ConfigurationServiceTests
{
    private readonly ISecretStore _secretStore = Substitute.For<ISecretStore>();

    private ConfigurationService Sut => new(
        _secretStore,
        new ImapInputConfigurationValidator(),
        new NotionOutputConfigurationValidator());

    private static ImapInputConfiguration ImapConfig(
        string host = "imap.example.com", string username = "user@example.com", int port = 993) => new()
    {
        Host = host,
        Port = port,
        Username = username,
        UseSsl = true,
        RetrievingFolder = "INBOX",
        ProcessedFolder = "Processed"
    };

    private static NotionOutputConfiguration NotionConfig(
        IEnumerable<Page>? pages = null, Guid? authTokenId = null) => new()
    {
        AuthTokenId = authTokenId ?? Guid.NewGuid(),
        Pages = pages ?? []
    };

    private async Task AssertNothingWrittenToKeyVault() =>
        await _secretStore.DidNotReceive()
            .SetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

    [Fact]
    public async Task CreateImap_returns_the_configuration_and_stores_the_password_under_its_id()
    {
        using var cts = new CancellationTokenSource();

        var result = await Sut.CreateImapInputConfigurationAsync(ImapConfig(), "s3cret", cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        var config = result.Value.ShouldNotBeNull();
        config.Host.ShouldBe("imap.example.com");
        config.Username.ShouldBe("user@example.com");
        await _secretStore.Received(1).SetSecretAsync(config.ImapPasswordId, "s3cret", cts.Token);
    }

    [Fact]
    public async Task CreateImap_reports_a_blank_host_and_writes_nothing()
    {
        var result = await Sut.CreateImapInputConfigurationAsync(ImapConfig(host: ""), "s3cret");

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfiguration.Host)));
        await AssertNothingWrittenToKeyVault();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public async Task CreateImap_reports_an_out_of_range_port_and_writes_nothing(int port)
    {
        var result = await Sut.CreateImapInputConfigurationAsync(ImapConfig(port: port), "s3cret");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfiguration.Port)));
        await AssertNothingWrittenToKeyVault();
    }

    [Fact]
    public async Task CreateNotion_returns_the_configuration_and_stores_the_token_under_the_auth_token_id()
    {
        var authTokenId = Guid.NewGuid();
        var pages = new List<Page>();
        using var cts = new CancellationTokenSource();

        var result = await Sut.CreateNotionOutputConfigurationAsync(
            NotionConfig(pages: pages, authTokenId: authTokenId), "ntn_token", cts.Token);

        result.Succeeded.ShouldBeTrue();
        var config = result.Value.ShouldNotBeNull();
        config.AuthTokenId.ShouldBe(authTokenId);
        config.Pages.ShouldBeSameAs(pages);
        await _secretStore.Received(1).SetSecretAsync(authTokenId.ToString(), "ntn_token", cts.Token);
    }

    [Fact]
    public async Task CreateNotion_reports_an_empty_auth_token_id_and_writes_nothing()
    {
        var result = await Sut.CreateNotionOutputConfigurationAsync(
            NotionConfig(authTokenId: Guid.Empty), "ntn_token");

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(NotionOutputConfiguration.AuthTokenId)));
        await AssertNothingWrittenToKeyVault();
    }
}
