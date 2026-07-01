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
    public void OutputHost_is_notion()
    {
        Config().OutputHost.ShouldBe(OutputHost.Notion);
    }

    [Fact]
    public void PartitionKey_is_the_document_id()
    {
        var config = Config() with { Id = "doc-1" };
        config.PartitionKey.ShouldBe("doc-1");
    }
}
