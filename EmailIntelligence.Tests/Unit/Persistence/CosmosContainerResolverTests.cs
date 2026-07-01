using EmailIntelligence.Domain.Persistence;
using EmailIntelligence.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Tests.Unit.Persistence;

public class CosmosContainerResolverTests : IDisposable
{
    private const string EmulatorKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private readonly CosmosClient _client = new("https://localhost:8081/", EmulatorKey);

    private CosmosContainerResolver CreateSut(params CosmosContainerRegistration[] registrations) =>
        new(_client,
            Options.Create(new CosmosOptions { DatabaseId = "db", AccountEndpoint = "https://localhost:8081/" }),
            registrations);

    private static CosmosContainerRegistration TestDocs() =>
        new(typeof(TestDoc), "test-docs", "/id");

    [Fact]
    public void Resolve_returns_container_for_registered_type()
    {
        var sut = CreateSut(TestDocs());

        var container = sut.Resolve<TestDoc>();

        container.ShouldNotBeNull();
        container.Id.ShouldBe("test-docs");
    }

    [Fact]
    public void Resolve_caches_the_container_handle()
    {
        var sut = CreateSut(TestDocs());

        sut.Resolve<TestDoc>().ShouldBeSameAs(sut.Resolve<TestDoc>());
    }

    [Fact]
    public void Resolve_unregistered_type_throws_with_actionable_message()
    {
        var sut = CreateSut(TestDocs());

        var ex = Should.Throw<InvalidOperationException>(() => sut.Resolve<UnregisteredDoc>());
        ex.Message.ShouldContain(nameof(UnregisteredDoc));
    }

    [Fact]
    public void Registrations_exposes_all_registered_types()
    {
        var sut = CreateSut(TestDocs());

        var registration = sut.Registrations.ShouldHaveSingleItem();
        registration.DocumentType.ShouldBe(typeof(TestDoc));
        registration.ContainerName.ShouldBe("test-docs");
    }

    public void Dispose() => _client.Dispose();

    private sealed record TestDoc : Document
    {
        public override string PartitionKey => Id;
    }

    private sealed record UnregisteredDoc : Document
    {
        public override string PartitionKey => "x";
    }
}
