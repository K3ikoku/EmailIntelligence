using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Functions.Functions;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using EmailIntelligence.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailIntelligence.Tests.Unit.Functions;

public class FeedProfileHttpFunctionsTests
{
    private readonly IFeedProfileService _service = Substitute.For<IFeedProfileService>();

    private FeedProfileHttpFunctions Sut => new(_service, NullLogger<FeedProfileHttpFunctions>.Instance);

    private const string ValidBody = """
                                     { "name": "Tech", "inputId": "input-1", "outputId": "output-1", "front": 3 }
                                     """;

    [Fact]
    public async Task UpsertFeedProfile_returns_ok_with_the_saved_profile()
    {
        var saved = new FeedProfile
        {
            Name = "Tech", InputId = "input-1", OutputId = "output-1",
            MatchRule = [], Processing = [], Front = Front.It
        };
        _service.UpsertFeedProfileAsync(null!)
            .ReturnsForAnyArgs(ConfigurationResult<FeedProfile>.Success(saved));

        var result = await Sut.UpsertFeedProfile(HttpRequestFactory.Json(ValidBody));

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(saved);
    }

    [Fact]
    public async Task UpsertFeedProfile_maps_the_body_to_the_service()
    {
        _service.UpsertFeedProfileAsync(null!)
            .ReturnsForAnyArgs(ci => ConfigurationResult<FeedProfile>.Success(ci.Arg<FeedProfile>()));

        await Sut.UpsertFeedProfile(HttpRequestFactory.Json(ValidBody));

        await _service.Received(1).UpsertFeedProfileAsync(
            Arg.Is<FeedProfile>(p =>
                p.Name == "Tech" && p.InputId == "input-1" && p.OutputId == "output-1" && p.Front == Front.It),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertFeedProfile_returns_bad_request_and_calls_nothing_when_the_body_is_missing()
    {
        var result = await Sut.UpsertFeedProfile(HttpRequestFactory.Json(null));

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _service.DidNotReceiveWithAnyArgs().UpsertFeedProfileAsync(null!);
    }

    [Fact]
    public async Task UpsertFeedProfile_surfaces_service_validation_errors_as_bad_request()
    {
        _service.UpsertFeedProfileAsync(null!)
            .ReturnsForAnyArgs(ConfigurationResult<FeedProfile>.Failure(["InputId is required."]));

        var result = await Sut.UpsertFeedProfile(
            HttpRequestFactory.Json("""{ "name": "Tech", "outputId": "output-1", "front": 3 }"""));

        var bad = result.ShouldBeOfType<BadRequestObjectResult>();
        bad.Value.ShouldBeAssignableTo<IEnumerable<string>>()!.ShouldContain("InputId is required.");
    }

    [Fact]
    public async Task DeleteFeedProfile_returns_no_content_when_the_profile_existed()
    {
        _service.DeleteFeedProfileAsync("feed-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.DeleteFeedProfile(HttpRequestFactory.Json(null), "feed-1");

        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteFeedProfile_returns_not_found_when_the_profile_was_absent()
    {
        _service.DeleteFeedProfileAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.DeleteFeedProfile(HttpRequestFactory.Json(null), "missing");

        result.ShouldBeOfType<NotFoundResult>();
    }
}