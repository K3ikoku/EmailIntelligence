using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class FeedProfileValidatorTests
{
    private static FeedProfile Valid() => new()
    {
        Name = "Tech newsletters",
        InputId = "input-1",
        OutputId = "output-1",
        MatchRule = [],
        Processing = [],
        Front = Front.It
    };

    private static bool Validate(FeedProfile profile, out string failures)
    {
        var result = new FeedProfileValidator().Validate(null, profile);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Valid_profile_passes()
    {
        Validate(Valid(), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_fails(string name)
    {
        Validate(Valid() with { Name = name }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(FeedProfile.Name));
    }

    [Fact]
    public void Blank_input_id_fails()
    {
        Validate(Valid() with { InputId = "" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(FeedProfile.InputId));
    }

    [Fact]
    public void Blank_output_id_fails()
    {
        Validate(Valid() with { OutputId = "" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(FeedProfile.OutputId));
    }

    [Fact]
    public void Undefined_front_fails()
    {
        Validate(Valid() with { Front = (Front)999 }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(FeedProfile.Front));
    }
}
