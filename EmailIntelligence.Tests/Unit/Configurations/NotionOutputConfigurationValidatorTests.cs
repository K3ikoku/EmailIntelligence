using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class NotionOutputConfigurationValidatorTests
{
    private static NotionOutputConfiguration Valid() => new()
    {
        AuthTokenId = Guid.NewGuid(),
        Pages = []
    };

    private static bool Validate(NotionOutputConfiguration configuration, out string failures)
    {
        var result = new NotionOutputConfigurationValidator().Validate(null, configuration);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Valid_configuration_passes()
    {
        Validate(Valid(), out _).ShouldBeTrue();
    }

    [Fact]
    public void Empty_auth_token_id_fails()
    {
        Validate(Valid() with { AuthTokenId = Guid.Empty }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOutputConfiguration.AuthTokenId));
    }

    [Fact]
    public void Null_pages_fail()
    {
        Validate(Valid() with { Pages = null! }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOutputConfiguration.Pages));
    }
}
