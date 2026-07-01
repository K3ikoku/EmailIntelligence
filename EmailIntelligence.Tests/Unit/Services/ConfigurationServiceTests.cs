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
        new ImapInputConfigurationRequestValidator(),
        new NotionOutputConfigurationRequestValidator());

    private static ImapInputConfigurationRequest ImapRequest(
        string username = "user@example.com", string password = "s3cret", int port = 993) => new()
    {
        Host = "imap.example.com",
        Port = port,
        Username = username,
        Password = password,
        UseSsl = true,
        RetrievingFolder = "INBOX",
        ProcessedFolder = "Processed"
    };

    private static NotionOutputConfigurationRequest NotionRequest(
        string authToken = "ntn_token", IEnumerable<Page>? pages = null, Guid? authTokenId = null) => new()
    {
        AuthTokenId = authTokenId ?? Guid.NewGuid(),
        AuthToken = authToken,
        Pages = pages ?? []
    };

    private async Task AssertNothingWrittenToKeyVault() =>
        await _secretStore.DidNotReceive()
            .SetSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

    [Fact]
    public async Task CreateImap_builds_the_configuration_and_stores_the_password()
    {
        using var cts = new CancellationTokenSource();

        var result = await Sut.CreateImapInputConfigurationAsync(ImapRequest(), cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        var config = result.Value.ShouldNotBeNull();
        config.Host.ShouldBe("imap.example.com");
        config.Username.ShouldBe("user@example.com");
        await _secretStore.Received(1).SetSecretAsync(config.ImapPasswordId, "s3cret", cts.Token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateImap_reports_a_blank_password_and_writes_nothing(string password)
    {
        var result = await Sut.CreateImapInputConfigurationAsync(ImapRequest(password: password));

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfigurationRequest.Password)));
        await AssertNothingWrittenToKeyVault();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public async Task CreateImap_reports_an_out_of_range_port_and_writes_nothing(int port)
    {
        var result = await Sut.CreateImapInputConfigurationAsync(ImapRequest(port: port));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains(nameof(ImapInputConfigurationRequest.Port)));
        await AssertNothingWrittenToKeyVault();
    }

    [Fact]
    public async Task CreateNotion_uses_the_requests_auth_token_id_and_stores_the_token_under_it()
    {
        var authTokenId = Guid.NewGuid();
        var pages = new List<Page>();
        using var cts = new CancellationTokenSource();

        var result = await Sut.CreateNotionOutputConfigurationAsync(
            NotionRequest(pages: pages, authTokenId: authTokenId), cts.Token);

        result.Succeeded.ShouldBeTrue();
        var config = result.Value.ShouldNotBeNull();
        config.AuthTokenId.ShouldBe(authTokenId);
        config.Pages.ShouldBeSameAs(pages);
        await _secretStore.Received(1).SetSecretAsync(authTokenId.ToString(), "ntn_token", cts.Token);
    }

    [Fact]
    public async Task CreateNotion_reports_an_empty_auth_token_id_and_writes_nothing()
    {
        var result = await Sut.CreateNotionOutputConfigurationAsync(NotionRequest(authTokenId: Guid.Empty));

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains(nameof(NotionOutputConfigurationRequest.AuthTokenId)));
        await AssertNothingWrittenToKeyVault();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateNotion_reports_a_blank_token_and_writes_nothing(string token)
    {
        var result = await Sut.CreateNotionOutputConfigurationAsync(NotionRequest(authToken: token));

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(NotionOutputConfigurationRequest.AuthToken)));
        await AssertNothingWrittenToKeyVault();
    }
}
