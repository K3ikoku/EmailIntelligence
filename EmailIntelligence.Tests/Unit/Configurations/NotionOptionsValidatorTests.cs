using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class NotionOptionsValidatorTests
{
    private static NotionOptions Valid() => new()
    {
        AuthToken = "token",
        DatabaseId = "db",
        Properties = [new NotionPropertyOptions { Name = "Name", Type = NotionPropertyType.Title, Value = null }]
    };

    private static bool Validate(NotionOptions options, out string failures)
    {
        var result = new NotionOptionsValidator().Validate(null, options);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Valid_options_pass()
    {
        Validate(Valid(), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_auth_token_fails(string token)
    {
        Validate(Valid() with { AuthToken = token }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOptions.AuthToken));
    }

    [Fact]
    public void Missing_database_id_fails()
    {
        Validate(Valid() with { DatabaseId = "" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOptions.DatabaseId));
    }

    [Fact]
    public void Empty_properties_fails()
    {
        Validate(Valid() with { Properties = [] }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(NotionOptions.Properties));
    }

    [Fact]
    public void Multiple_problems_are_all_reported()
    {
        Validate(new NotionOptions { AuthToken = "", DatabaseId = "", Properties = [] }, out var failures)
            .ShouldBeFalse();

        failures.ShouldContain(nameof(NotionOptions.AuthToken));
        failures.ShouldContain(nameof(NotionOptions.DatabaseId));
        failures.ShouldContain(nameof(NotionOptions.Properties));
    }
}
