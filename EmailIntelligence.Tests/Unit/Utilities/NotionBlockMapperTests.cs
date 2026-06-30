using EmailIntelligence.Domain.Entities.Content;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Infrastructure.Utilities;
using Notion.Client;
using DomainImageBlock = EmailIntelligence.Domain.Entities.Content.ImageBlock;
using NotionImageBlock = Notion.Client.ImageBlock;

namespace EmailIntelligence.Tests.Unit.Utilities;

public class NotionBlockMapperTests
{
    private static TextBlock TextBlock(ContentBlockType type, params RichText[] runs) =>
        new() { Type = type, RichText = runs };

    private static RichText Run(string text, string? href = null) => new() { Text = text, Href = href };

    [Fact]
    public void Maps_heading_one()
    {
        var block = TextBlock(ContentBlockType.Heading1, Run("Title")).ToNotionBlock();

        var run = block.ShouldBeOfType<HeadingOneBlock>().Heading_1.RichText
            .ShouldHaveSingleItem().ShouldBeOfType<RichTextText>();
        run.Text.Content.ShouldBe("Title");
    }

    [Fact]
    public void Maps_heading_two()
    {
        TextBlock(ContentBlockType.Heading2, Run("x")).ToNotionBlock().ShouldBeOfType<HeadingTwoBlock>();
    }

    [Fact]
    public void Maps_heading_three()
    {
        TextBlock(ContentBlockType.Heading3, Run("x")).ToNotionBlock().ShouldBeOfType<HeadingThreeBlock>();
    }

    [Fact]
    public void Maps_bulleted_list_item()
    {
        TextBlock(ContentBlockType.BulletedListItem, Run("x")).ToNotionBlock()
            .ShouldBeOfType<BulletedListItemBlock>();
    }

    [Fact]
    public void Maps_numbered_list_item()
    {
        TextBlock(ContentBlockType.NumberedListItem, Run("x")).ToNotionBlock()
            .ShouldBeOfType<NumberedListItemBlock>();
    }

    [Theory]
    [InlineData(ContentBlockType.Paragraph)]
    [InlineData(ContentBlockType.Image)]
    public void Maps_other_text_blocks_to_paragraph(ContentBlockType type)
    {
        var block = TextBlock(type, Run("body")).ToNotionBlock();

        var run = block.ShouldBeOfType<ParagraphBlock>().Paragraph.RichText
            .ShouldHaveSingleItem().ShouldBeOfType<RichTextText>();
        run.Text.Content.ShouldBe("body");
    }

    [Fact]
    public void Run_without_href_has_no_link()
    {
        var block = TextBlock(ContentBlockType.Paragraph, Run("plain")).ToNotionBlock();

        var run = (RichTextText)block.ShouldBeOfType<ParagraphBlock>().Paragraph.RichText.Single();
        run.Text.Link.ShouldBeNull();
    }

    [Fact]
    public void Run_with_href_carries_link_url()
    {
        var block = TextBlock(ContentBlockType.Paragraph, Run("here", "https://x.com")).ToNotionBlock();

        var run = (RichTextText)block.ShouldBeOfType<ParagraphBlock>().Paragraph.RichText.Single();
        run.Text.Link.Url.ShouldBe("https://x.com");
    }

    [Fact]
    public void Maps_image_block_to_external_file_with_caption()
    {
        var domain = new DomainImageBlock
        {
            Url = "https://img/a.png",
            Caption = [Run("a caption")]
        };

        var external = domain.ToNotionBlock().ShouldBeOfType<NotionImageBlock>()
            .Image.ShouldBeOfType<ExternalFile>();

        external.External.Url.ShouldBe("https://img/a.png");
        var caption = external.Caption.ShouldHaveSingleItem().ShouldBeOfType<RichTextText>();
        caption.Text.Content.ShouldBe("a caption");
    }

    [Fact]
    public void Unsupported_block_type_throws()
    {
        ContentBlock unknown = new UnknownBlock();
        Should.Throw<NotSupportedException>(() => unknown.ToNotionBlock());
    }

    private sealed record UnknownBlock : ContentBlock
    {
        public override ContentBlockType Type { get; init; } = ContentBlockType.Paragraph;
    }
}
