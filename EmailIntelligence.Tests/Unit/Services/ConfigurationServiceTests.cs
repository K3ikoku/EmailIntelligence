using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Secrets;
using EmailIntelligence.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailIntelligence.Tests.Unit.Services;

public class ConfigurationServiceTests
{
    private readonly ISecretStore _secretStore = Substitute.For<ISecretStore>();

    private readonly IRepository<ConnectorConfiguration> _connectors =
        Substitute.For<IRepository<ConnectorConfiguration>>();

    private ConfigurationService Sut => new(
        _secretStore,
        _connectors,
        new ImapInputConfigurationValidator(),
        new NotionOutputConfigurationValidator(),
        NullLogger<ConfigurationService>.Instance);

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

    private async Task AssertNothingPersisted() =>
        await _connectors.DidNotReceive()
            .UpsertAsync(Arg.Any<ConnectorConfiguration>(), Arg.Any<CancellationToken>());

    [Fact]
    public async Task UpsertImap_persists_the_configuration_and_stores_the_password_under_its_id()
    {
        using var cts = new CancellationTokenSource();
        var config = ImapConfig();

        var result = await Sut.UpsertImapInputConfigurationAsync(config, "s3cret", cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Value.ShouldBe(config);
        await _secretStore.Received(1).SetSecretAsync(config.ImapPasswordId, "s3cret", cts.Token);
        await _connectors.Received(1).UpsertAsync(config, cts.Token);
    }

    [Fact]
    public async Task UpsertImap_reports_a_blank_host_and_writes_nothing()
    {
        var result = await Sut.UpsertImapInputConfigurationAsync(ImapConfig(host: ""), "s3cret");

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfiguration.Host)));
        await AssertNothingWrittenToKeyVault();
        await AssertNothingPersisted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public async Task UpsertImap_reports_an_out_of_range_port_and_writes_nothing(int port)
    {
        var result = await Sut.UpsertImapInputConfigurationAsync(ImapConfig(port: port), "s3cret");

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfiguration.Port)));
        await AssertNothingWrittenToKeyVault();
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task UpsertNotion_persists_the_configuration_and_stores_the_token_under_the_auth_token_id()
    {
        var authTokenId = Guid.NewGuid();
        var pages = new List<Page>();
        using var cts = new CancellationTokenSource();
        var config = NotionConfig(pages: pages, authTokenId: authTokenId);

        var result = await Sut.UpsertNotionOutputConfigurationAsync(config, "ntn_token", cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(config);
        await _secretStore.Received(1).SetSecretAsync(authTokenId.ToString(), "ntn_token", cts.Token);
        await _connectors.Received(1).UpsertAsync(config, cts.Token);
    }

    [Fact]
    public async Task UpsertNotion_reports_an_empty_auth_token_id_and_writes_nothing()
    {
        var result = await Sut.UpsertNotionOutputConfigurationAsync(
            NotionConfig(authTokenId: Guid.Empty), "ntn_token");

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(NotionOutputConfiguration.AuthTokenId)));
        await AssertNothingWrittenToKeyVault();
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task DeleteConnector_returns_false_when_it_does_not_exist()
    {
        _connectors.GetAsync("missing", "missing", Arg.Any<CancellationToken>())
            .Returns((ConnectorConfiguration?)null);

        var deleted = await Sut.DeleteConnectorAsync("missing");

        deleted.ShouldBeFalse();
        await _connectors.DidNotReceive()
            .DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _secretStore.DidNotReceive().DeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteConnector_removes_an_imap_configuration_and_its_password_secret()
    {
        using var cts = new CancellationTokenSource();
        var config = ImapConfig() with { Id = "imap-1" };
        _connectors.GetAsync("imap-1", "imap-1", cts.Token).Returns(config);

        var deleted = await Sut.DeleteConnectorAsync("imap-1", cts.Token);

        deleted.ShouldBeTrue();
        await _secretStore.Received(1).DeleteSecretAsync(config.ImapPasswordId, cts.Token);
        await _connectors.Received(1).DeleteAsync("imap-1", "imap-1", cts.Token);
    }

    [Fact]
    public async Task DeleteConnector_removes_a_notion_configuration_and_its_token_secret()
    {
        using var cts = new CancellationTokenSource();
        var authTokenId = Guid.NewGuid();
        var config = NotionConfig(authTokenId: authTokenId) with { Id = "notion-1" };
        _connectors.GetAsync("notion-1", "notion-1", cts.Token).Returns(config);

        var deleted = await Sut.DeleteConnectorAsync("notion-1", cts.Token);

        deleted.ShouldBeTrue();
        await _secretStore.Received(1).DeleteSecretAsync(authTokenId.ToString(), cts.Token);
        await _connectors.Received(1).DeleteAsync("notion-1", "notion-1", cts.Token);
    }
}
