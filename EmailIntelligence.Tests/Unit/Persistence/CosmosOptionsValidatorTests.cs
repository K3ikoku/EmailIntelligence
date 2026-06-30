using EmailIntelligence.Infrastructure.Persistence.Cosmos;

namespace EmailIntelligence.Tests.Unit.Persistence;

public class CosmosOptionsValidatorTests
{
    private static bool Validate(CosmosOptions options, out string failures)
    {
        var result = new CosmosOptionsValidator().Validate(null, options);
        failures = result.FailureMessage ?? string.Empty;
        return result.Succeeded;
    }

    [Fact]
    public void Account_endpoint_with_database_passes()
    {
        Validate(new CosmosOptions { DatabaseId = "db", AccountEndpoint = "https://acct/" }, out _)
            .ShouldBeTrue();
    }

    [Fact]
    public void Connection_string_with_database_passes()
    {
        Validate(new CosmosOptions { DatabaseId = "db", ConnectionString = "AccountEndpoint=...;" }, out _)
            .ShouldBeTrue();
    }

    [Fact]
    public void Missing_database_id_fails()
    {
        Validate(new CosmosOptions { DatabaseId = "", AccountEndpoint = "https://acct/" }, out var failures)
            .ShouldBeFalse();
        failures.ShouldContain(nameof(CosmosOptions.DatabaseId));
    }

    [Fact]
    public void Neither_endpoint_nor_connection_string_fails()
    {
        Validate(new CosmosOptions { DatabaseId = "db" }, out var failures).ShouldBeFalse();
        failures.ShouldContain(nameof(CosmosOptions.AccountEndpoint));
    }
}
