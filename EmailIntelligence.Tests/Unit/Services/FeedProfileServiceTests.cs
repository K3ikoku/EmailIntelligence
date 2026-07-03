using System.Linq.Expressions;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailIntelligence.Tests.Unit.Services;

public class FeedProfileServiceTests
{
    private readonly IRepository<FeedProfile> _repository = Substitute.For<IRepository<FeedProfile>>();

    private FeedProfileService Sut =>
        new(new FeedProfileValidator(), _repository, NullLogger<FeedProfileService>.Instance);

    private static FeedProfile Profile(
        string name = "Tech", string inputId = "input-1", string outputId = "output-1") => new()
    {
        Name = name,
        InputId = inputId,
        OutputId = outputId,
        MatchRule = [],
        Processing = [],
        Front = Front.It
    };

    [Fact]
    public async Task Upsert_persists_the_profile_and_returns_the_stored_item()
    {
        var profile = Profile();
        _repository.UpsertAsync(profile, Arg.Any<CancellationToken>()).Returns(profile);
        using var cts = new CancellationTokenSource();

        var result = await Sut.UpsertFeedProfileAsync(profile, cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(profile);
        await _repository.Received(1).UpsertAsync(profile, cts.Token);
    }

    [Fact]
    public async Task Upsert_reports_a_blank_input_id_and_persists_nothing()
    {
        var result = await Sut.UpsertFeedProfileAsync(Profile(inputId: ""));

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(FeedProfile.InputId)));
        await _repository.DidNotReceive().UpsertAsync(Arg.Any<FeedProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_returns_false_when_no_profile_matches()
    {
        _repository.QueryAsync(Arg.Any<Expression<Func<FeedProfile, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FeedProfile>)[]);

        var deleted = await Sut.DeleteFeedProfileAsync("missing");

        deleted.ShouldBeFalse();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_removes_the_matching_profile_using_its_partition_key()
    {
        using var cts = new CancellationTokenSource();
        var profile = Profile(inputId: "input-9") with { Id = "feed-1" };
        _repository.QueryAsync(Arg.Any<Expression<Func<FeedProfile, bool>>>(), cts.Token)
            .Returns((IReadOnlyList<FeedProfile>)[profile]);

        var deleted = await Sut.DeleteFeedProfileAsync("feed-1", cts.Token);

        deleted.ShouldBeTrue();
        await _repository.Received(1).DeleteAsync("feed-1", "input-9", cts.Token);
    }
}
