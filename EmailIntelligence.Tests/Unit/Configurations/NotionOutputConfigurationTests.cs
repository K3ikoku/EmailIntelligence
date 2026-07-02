using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Tests.Unit.Configurations;

public class NotionOutputConfigurationTests
{
    private static NotionOutputConfiguration Config() => new()
    {
        AuthTokenId = Guid.NewGuid(),
        Pages = []
    };

    [Fact]
    public void Connector_is_notion_output()
    {
        var config = Config();
        config.Connector.ShouldBe(Connector.Notion);
        config.Direction.ShouldBe(ConnectorDirection.Output);
    }

    [Fact]
    public void PartitionKey_is_the_document_id()
    {
        var config = Config() with { Id = "doc-1" };
        config.PartitionKey.ShouldBe("doc-1");
    }
}
