using EmailIntelligence.Domain.Entities.Configurations;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class ImapInputConfigurationRequestValidatorTests
{
    private static ImapInputConfigurationRequest Valid() => new()
    {
        Host = "imap.example.com",
        Port = 993,
        Username = "user@example.com",
        Password = "s3cret",
        UseSsl = true,
        RetrievingFolder = "INBOX",
        ProcessedFolder = "Processed"
    };

    private static bool Validate(ImapInputConfigurationRequest request, out string failures)
    {
        var result = new ImapInputConfigurationRequestValidator().Validate(null, request);
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
    public void Missing_host_fails(string host)
    {
        Validate(Valid() with { Host = host }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.Host));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_username_fails(string username)
    {
        Validate(Valid() with { Username = username }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_password_fails(string password)
    {
        Validate(Valid() with { Password = password }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.Password));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Out_of_range_port_fails(int port)
    {
        Validate(Valid() with { Port = port }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.Port));
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
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.RetrievingFolder));
        failures.ShouldContain(nameof(ImapInputConfigurationRequest.ProcessedFolder));
    }
}
