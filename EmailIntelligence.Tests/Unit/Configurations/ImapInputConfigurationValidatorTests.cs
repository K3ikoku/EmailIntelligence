using EmailIntelligence.Domain.Entities.Configurations;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class ImapInputConfigurationValidatorTests
{
    private static ImapInputConfiguration Valid() => new()
    {
        Host = "imap.example.com",
        Port = 993,
        Username = "user@example.com",
        UseSsl = true,
        RetrievingFolder = "INBOX",
        ProcessedFolder = "Processed"
    };

    private static bool Validate(ImapInputConfiguration configuration, out string failures)
    {
        var result = new ImapInputConfigurationValidator().Validate(null, configuration);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Valid_configuration_passes()
    {
        Validate(Valid(), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_host_fails(string host)
    {
        Validate(Valid() with { Host = host }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfiguration.Host));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_username_fails(string username)
    {
        Validate(Valid() with { Username = username }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfiguration.Username));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Out_of_range_port_fails(int port)
    {
        Validate(Valid() with { Port = port }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfiguration.Port));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(993)]
    [InlineData(65535)]
    public void Port_at_the_boundaries_passes(int port)
    {
        Validate(Valid() with { Port = port }, out _).ShouldBeTrue();
    }

    [Fact]
    public void Missing_folders_fail()
    {
        Validate(Valid() with { RetrievingFolder = "", ProcessedFolder = "" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfiguration.RetrievingFolder));
        failures.ShouldContain(nameof(ImapInputConfiguration.ProcessedFolder));
    }

    [Fact]
    public void All_failures_are_reported_together()
    {
        Validate(
            Valid() with { Host = "", Username = "", RetrievingFolder = "", ProcessedFolder = "", Port = 0 },
            out var failures).ShouldBeFalse();

        failures.ShouldContain(nameof(ImapInputConfiguration.Host));
        failures.ShouldContain(nameof(ImapInputConfiguration.Username));
        failures.ShouldContain(nameof(ImapInputConfiguration.RetrievingFolder));
        failures.ShouldContain(nameof(ImapInputConfiguration.ProcessedFolder));
        failures.ShouldContain(nameof(ImapInputConfiguration.Port));
    }
}
