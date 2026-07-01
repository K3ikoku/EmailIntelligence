using EmailIntelligence.Domain.Entities.Configurations.Notion;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class NotionOutputConfigurationRequestValidatorTests
{
    private static NotionOutputConfigurationRequest Valid() => new()
    {
        AuthToken = "ntn_token",
        Pages = []
    };

    private static bool Validate(NotionOutputConfigurationRequest request, out string failures)
    {
        var result = new NotionOutputConfigurationRequestValidator().Validate(null, request);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Valid_request_passes()
    {
        Validate(Valid(), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_auth_token_fails(string token)
    {
        Validate(Valid() with { AuthToken = token }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOutputConfigurationRequest.AuthToken));
    }

    [Fact]
    public void Null_pages_fail()
    {
        Validate(Valid() with { Pages = null! }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOutputConfigurationRequest.Pages));
    }

    [Fact]
    public void Empty_auth_token_id_fails()
    {
        Validate(Valid() with { AuthTokenId = Guid.Empty }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOutputConfigurationRequest.AuthTokenId));
    }
}
