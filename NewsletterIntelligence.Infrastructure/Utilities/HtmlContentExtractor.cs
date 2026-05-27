using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NewsletterIntelligence.Domain.Entities.Content;
using NewsletterIntelligence.Domain.Enums;

namespace NewsletterIntelligence.Infrastructure.Utilities;

/// <summary>
/// Parses raw email HTML and returns a flat list of Notion-friendly content blocks.
/// Only headings, paragraphs, lists, and links are kept; everything else
/// (script, style, images, layout/wrapper tags) is stripped.
/// </summary>
public static partial class HtmlContentExtractor 
{
    private static readonly Regex WhitespaceRegex = MyRegex();

    public static ExtractedContent Extract(string html)
    {
        var blocks = new List<ContentBlock>();

        if (string.IsNullOrWhiteSpace(html))
            return new ExtractedContent { Blocks = blocks };

        var doc = new HtmlDocument
        {
            OptionAutoCloseOnEnd = true,
            OptionFixNestedTags = true
        };
        doc.LoadHtml(html);

        var root = (HtmlNode?)doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

        Walk(root, blocks);
        return new ExtractedContent { Blocks = blocks };
    }

    private static void Walk(HtmlNode node, List<ContentBlock> blocks)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType != HtmlNodeType.Element)
                continue;

            var name = child.Name.ToLowerInvariant();

            switch (name)
            {
                case "script":
                case "style":
                case "noscript":
                case "head":
                case "img":
                case "svg":
                case "picture":
                case "video":
                case "audio":
                case "iframe":
                    continue;

                case "h1":
                case "h2":
                case "h3":
                    AddTextBlock(child, MapHeading(name), blocks);
                    break;

                // Notion only supports heading levels 1–3; map h4–h6 to h3.
                case "h4":
                case "h5":
                case "h6":
                    AddTextBlock(child, ContentBlockType.Heading3, blocks);
                    break;

                case "p":
                    AddTextBlock(child, ContentBlockType.Paragraph, blocks);
                    break;

                case "ul":
                    AddListItems(child, ContentBlockType.BulletedListItem, blocks);
                    break;

                case "ol":
                    AddListItems(child, ContentBlockType.NumberedListItem, blocks);
                    break;

                case "br":
                    continue;

                default:
                    // Container/layout element: recurse into its children.
                    Walk(child, blocks);
                    break;
            }
        }
    }

    private static void AddListItems(HtmlNode listNode, ContentBlockType itemType, List<ContentBlock> blocks)
    {
        foreach (var li in listNode.ChildNodes)
        {
            if (li.NodeType != HtmlNodeType.Element ||
                !string.Equals(li.Name, "li", StringComparison.OrdinalIgnoreCase))
                continue;

            AddTextBlock(li, itemType, blocks);

            // Handle nested lists by appending their items after the parent item.
            foreach (var nested in li.ChildNodes)
            {
                if (nested.NodeType != HtmlNodeType.Element) continue;
                var n = nested.Name.ToLowerInvariant();
                if (n == "ul") AddListItems(nested, ContentBlockType.BulletedListItem, blocks);
                else if (n == "ol") AddListItems(nested, ContentBlockType.NumberedListItem, blocks);
            }
        }
    }

    private static void AddTextBlock(HtmlNode node, ContentBlockType type, List<ContentBlock> blocks)
    {
        var merged = CollectAndMerge(node);
        if (merged.Count == 0)
            return;

        blocks.Add(new TextBlock { Type = type, RichText = merged });
    }

    private static IReadOnlyList<RichText> CollectAndMerge(HtmlNode node)
    {
        var runs = new List<RichText>();
        CollectRichText(node, currentHref: null, runs);
        return MergeAdjacent(runs);
    }

    private static void TryAddImage(HtmlNode img, IReadOnlyList<RichText>? caption, List<ContentBlock> blocks)
    {
        // Prefer src; some newsletters lazy-load via data-src / data-original.
        var url = FirstNonEmpty(
            img.GetAttributeValue("src", null),
            img.GetAttributeValue("data-src", null),
            img.GetAttributeValue("data-original", null));

        if (string.IsNullOrWhiteSpace(url)) return;

        // Skip inline data URIs and obvious tracking pixels / spacers.
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;

        if (int.TryParse(img.GetAttributeValue("width", ""), out var w) &&
            int.TryParse(img.GetAttributeValue("height", ""), out var h) &&
            w <= 1 && h <= 1)
        {
            return;
        }

        var alt = img.GetAttributeValue("alt", null);
        if (string.IsNullOrWhiteSpace(alt)) alt = null;

        blocks.Add(new ImageBlock
        {
            Url = url,
            AltText = alt,
            Caption = caption ?? Array.Empty<RichText>(),
        });
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v)) return v;
        return null;
    }

    private static void CollectRichText(HtmlNode node, string? currentHref, List<RichText> runs)
    {
        foreach (var child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                case HtmlNodeType.Text:
                {
                    var text = Normalize(HtmlEntity.DeEntitize(child.InnerText));
                    if (text.Length > 0)
                        runs.Add(new RichText
                        {
                            Text = text,
                            Href = currentHref
                        });
                    break;
                }
                case HtmlNodeType.Element:
                {
                    var name = child.Name.ToLowerInvariant();

                    switch (name)
                    {
                        // Don't descend into nested block-level lists here; the
                        // outer walker handles them so they become their own blocks.
                        case "ul" or "ol" or "script" or "style":
                            continue;
                        case "br":
                            runs.Add(new RichText
                            {
                                Text = "\n",
                                Href = currentHref
                            });
                            continue;
                        case "a":
                        {
                            var href = child.GetAttributeValue("href", null);
                            CollectRichText(child, string.IsNullOrWhiteSpace(href) ? currentHref : href, runs);
                            continue;
                        }
                    }

                    CollectRichText(child, currentHref, runs);
                    break;
                }
            }
        }
    }

    private static IReadOnlyList<RichText> MergeAdjacent(List<RichText> runs)
    {
        var result = new List<RichText>(runs.Count);
        foreach (var run in runs)
        {
            if (result.Count > 0 && result[^1].Href == run.Href)
            {
                var prev = result[^1];
                result[^1] = prev with { Text = JoinText(prev.Text, run.Text) };
            }
            else
            {
                result.Add(run);
            }
        }

        // Trim leading/trailing whitespace on the block as a whole.
        if (result.Count > 0)
        {
            result[0] = result[0] with { Text = result[0].Text.TrimStart() };
            result[^1] = result[^1] with { Text = result[^1].Text.TrimEnd() };
            result.RemoveAll(r => r.Text.Length == 0);
        }

        return result;
    }

    private static string JoinText(string a, string b)
    {
        if (a.Length == 0) return b;
        if (b.Length == 0) return a;

        var endsWithSpace = char.IsWhiteSpace(a[^1]);
        var startsWithSpace = char.IsWhiteSpace(b[0]);

        if (endsWithSpace || startsWithSpace)
            return a + b;

        // Insert a single space between adjacent runs that came from separate
        // inline elements so words don't smash together.
        var sb = new StringBuilder(a.Length + b.Length + 1);
        sb.Append(a).Append(' ').Append(b);
        return sb.ToString();
    }

    private static string Normalize(string text) =>
        WhitespaceRegex.Replace(text, " ");

    private static ContentBlockType MapHeading(string tag) => tag switch
    {
        "h1" => ContentBlockType.Heading1,
        "h2" => ContentBlockType.Heading2,
        _    => ContentBlockType.Heading3
    };
    
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}

