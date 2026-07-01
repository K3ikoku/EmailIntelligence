using System.Text.RegularExpressions;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Domain.Enums;

namespace EmailIntelligence.Tests.Unit.Persistence;

public class CosmosDocumentTests
{
    private static readonly Regex CompactGuid = new("^[0-9a-f]{32}$");

    private static FeedProfile FeedProfile() => new()
    {
        Name = "TLDR",
        InputId = "input-1",
        OutputId = "output-1",
        MatchRule = [],
        Processing = [],
        Front = Front.Oklassificerat
    };

    [Fact]
    public void Document_id_defaults_to_a_compact_guid()
    {
        FeedProfile().Id.ShouldMatch(CompactGuid.ToString());
    }

    [Fact]
    public void Each_document_gets_a_distinct_id()
    {
        FeedProfile().Id.ShouldNotBe(FeedProfile().Id);
    }

    [Fact]
    public void FeedProfile_partitions_by_input_id()
    {
        FeedProfile().PartitionKey.ShouldBe("input-1");
    }

    [Fact]
    public void FeedProfile_is_enabled_by_default()
    {
        FeedProfile().Enabled.ShouldBeTrue();
    }
}
