using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Functions.Functions;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using EmailIntelligence.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailIntelligence.Tests.Unit.Functions;

public class ConfigurationHttpFunctionsTests
{
    private readonly IConfigurationService _service = Substitute.For<IConfigurationService>();

    private ConfigurationHttpFunctions Sut => new(_service, NullLogger<ConfigurationHttpFunctions>.Instance);

    private const string ValidImapBody = """
                                         {
                                           "host": "imap.example.com",
                                           "port": 993,
                                           "username": "user@example.com",
                                           "password": "s3cret",
                                           "useSsl": true,
                                           "retrievingFolder": "INBOX",
                                           "processedFolder": "Processed"
                                         }
                                         """;

    [Fact]
    public async Task UpsertImap_returns_ok_with_the_saved_configuration()
    {
        var saved = new ImapInputConfiguration
        {
            Host = "imap.example.com", Port = 993, Username = "user@example.com",
            UseSsl = true, RetrievingFolder = "INBOX", ProcessedFolder = "Processed"
        };
        _service.UpsertImapInputConfigurationAsync(null!, null!)
            .ReturnsForAnyArgs(ConfigurationResult<ImapInputConfiguration>.Success(saved));

        var result = await Sut.UpsertImapInputConfiguration(HttpRequestFactory.Json(ValidImapBody));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(saved);
    }

    [Fact]
    public async Task UpsertImap_maps_the_body_and_forwards_the_password_to_the_service()
    {
        _service.UpsertImapInputConfigurationAsync(null!, null!)
            .ReturnsForAnyArgs(ci =>
                ConfigurationResult<ImapInputConfiguration>.Success(ci.Arg<ImapInputConfiguration>()));

        await Sut.UpsertImapInputConfiguration(HttpRequestFactory.Json(ValidImapBody));

        await _service.Received(1).UpsertImapInputConfigurationAsync(
            Arg.Is<ImapInputConfiguration>(c =>
                c.Host == "imap.example.com" && c.Username == "user@example.com" && c.Port == 993),
            "s3cret",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertImap_returns_bad_request_and_calls_nothing_when_the_body_is_missing()
    {
        var result = await Sut.UpsertImapInputConfiguration(HttpRequestFactory.Json(null));

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _service.DidNotReceiveWithAnyArgs().UpsertImapInputConfigurationAsync(null!, null!);
    }

    [Fact]
    public async Task UpsertImap_returns_bad_request_and_calls_nothing_when_the_password_is_missing()
    {
        var body = """
                   { "host": "imap.example.com", "port": 993, "username": "user@example.com",
                     "useSsl": true, "retrievingFolder": "INBOX", "processedFolder": "Processed" }
                   """;

        var result = await Sut.UpsertImapInputConfiguration(HttpRequestFactory.Json(body));

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _service.DidNotReceiveWithAnyArgs().UpsertImapInputConfigurationAsync(null!, null!);
    }

    [Fact]
    public async Task UpsertNotion_returns_ok_with_the_saved_configuration()
    {
        var saved = new NotionOutputConfiguration { AuthTokenId = Guid.NewGuid(), Pages = [] };
        _service.UpsertNotionOutputConfigurationAsync(null!, null!)
            .ReturnsForAnyArgs(ConfigurationResult<NotionOutputConfiguration>.Success(saved));

        var result = await Sut.UpsertNotionOutputConfiguration(
            HttpRequestFactory.Json("""{ "authToken": "ntn_token", "pages": [] }"""));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(saved);
    }

    [Fact]
    public async Task UpsertNotion_forwards_the_auth_token_to_the_service()
    {
        _service.UpsertNotionOutputConfigurationAsync(null!, null!)
            .ReturnsForAnyArgs(ci =>
                ConfigurationResult<NotionOutputConfiguration>.Success(ci.Arg<NotionOutputConfiguration>()));

        await Sut.UpsertNotionOutputConfiguration(
            HttpRequestFactory.Json("""{ "authToken": "ntn_token", "pages": [] }"""));

        await _service.Received(1).UpsertNotionOutputConfigurationAsync(
            Arg.Any<NotionOutputConfiguration>(), "ntn_token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertNotion_returns_bad_request_and_calls_nothing_when_the_body_is_missing()
    {
        var result = await Sut.UpsertNotionOutputConfiguration(HttpRequestFactory.Json(null));

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _service.DidNotReceiveWithAnyArgs().UpsertNotionOutputConfigurationAsync(null!, null!);
    }

    [Fact]
    public async Task UpsertNotion_returns_bad_request_and_calls_nothing_when_the_auth_token_is_missing()
    {
        var result = await Sut.UpsertNotionOutputConfiguration(
            HttpRequestFactory.Json("""{ "pages": [] }"""));

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _service.DidNotReceiveWithAnyArgs().UpsertNotionOutputConfigurationAsync(null!, null!);
    }

    [Fact]
    public async Task DeleteConnector_returns_no_content_when_the_connector_existed()
    {
        _service.DeleteConnectorAsync("imap-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.DeleteConnector(HttpRequestFactory.Json(null), "imap-1");

        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteConnector_returns_not_found_when_the_connector_was_absent()
    {
        _service.DeleteConnectorAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.DeleteConnector(HttpRequestFactory.Json(null), "missing");

        result.ShouldBeOfType<NotFoundResult>();
    }
}