using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Services;

namespace EmailIntelligence.Tests.Unit.Services;

public class FeedProfileServiceTests
{
    private readonly IRepository<FeedProfile> _repository = Substitute.For<IRepository<FeedProfile>>();

    private FeedProfileService Sut => new(new FeedProfileValidator(), _repository);

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
    public async Task Create_persists_the_profile_and_returns_the_stored_item()
    {
        var profile = Profile();
        _repository.CreateAsync(profile, Arg.Any<CancellationToken>()).Returns(profile);
        using var cts = new CancellationTokenSource();

        var result = await Sut.CreateFeedProfileAsync(profile, cts.Token);

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe(profile);
        await _repository.Received(1).CreateAsync(profile, cts.Token);
    }

    [Fact]
    public async Task Create_reports_a_blank_input_id_and_persists_nothing()
    {
        var result = await Sut.CreateFeedProfileAsync(Profile(inputId: ""));

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldContain(e => e.Contains(nameof(FeedProfile.InputId)));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<FeedProfile>(), Arg.Any<CancellationToken>());
    }
}
