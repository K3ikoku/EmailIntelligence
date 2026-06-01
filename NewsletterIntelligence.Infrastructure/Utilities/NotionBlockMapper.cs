using NewsletterIntelligence.Domain.Entities.Content;
using NewsletterIntelligence.Domain.Enums;
using Notion.Client;
using DomainImageBlock = NewsletterIntelligence.Domain.Entities.Content.ImageBlock;
using NotionImageBlock = Notion.Client.ImageBlock;

namespace NewsletterIntelligence.Infrastructure.Utilities;

public static class NotionBlockMapper
{
    public static IBlock ToNotionBlock(this ContentBlock block) => block switch
    {
        DomainImageBlock img => new NotionImageBlock
        {
            Image = new ExternalFile
            {
                External = new ExternalFile.Info { Url = img.Url },
                Caption = img.Caption.Select(ToRichText).ToList()
            }
        },
        TextBlock tb => tb.Type switch
        {
            ContentBlockType.Heading1 => new HeadingOneBlock
            {
                Heading_1 = new HeadingOneBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            },
            ContentBlockType.Heading2 => new HeadingTwoBlock
            {
                Heading_2 = new HeadingTwoBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            },
            ContentBlockType.Heading3 => new HeadingThreeBlock
            {
                Heading_3 = new HeadingThreeBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            },
            ContentBlockType.BulletedListItem => new BulletedListItemBlock
            {
                BulletedListItem = new BulletedListItemBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            },
            ContentBlockType.NumberedListItem => new NumberedListItemBlock
            {
                NumberedListItem = new NumberedListItemBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            },
            _ => new ParagraphBlock
            {
                Paragraph = new ParagraphBlock.Info { RichText = tb.RichText.Select(ToRichText).ToList() }
            }
        },
        _ => throw new NotSupportedException($"Unsupported block type: {block.GetType().Name}")
    };

    private static RichTextBase ToRichText(RichText r) =>
        r.Href is not null
            ? new RichTextText { Text = new Text { Content = r.Text, Link = new Link { Url = r.Href } } }
            : new RichTextText { Text = new Text { Content = r.Text } };
}

