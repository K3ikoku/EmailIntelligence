using EmailIntelligence.Infrastructure.Services;

namespace EmailIntelligence.Tests.Unit.Services;

public class ConfigurationResultTests
{
    [Fact]
    public void Success_carries_the_value_and_no_errors()
    {
        var result = ConfigurationResult<string>.Success("built");

        result.Succeeded.ShouldBeTrue();
        result.Value.ShouldBe("built");
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_carries_the_errors_and_no_value()
    {
        var result = ConfigurationResult<string>.Failure(["missing host", "bad port"]);

        result.Succeeded.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Errors.ShouldBe(["missing host", "bad port"]);
    }
}
