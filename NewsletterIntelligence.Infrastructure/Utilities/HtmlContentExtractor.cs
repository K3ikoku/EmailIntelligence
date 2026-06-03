using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NewsletterIntelligence.Domain.Entities.Content;
using NewsletterIntelligence.Domain.Enums;

namespace NewsletterIntelligence.Infrastructure.Utilities;

/// <summary>
/// Parses raw email HTML and returns a flat list of Notion-friendly content blocks.
/// Headings, paragraphs, lists, and links are kept; everything else
/// (scripts, styles, images, hidden preview text, layout/wrapper tags) is stripped.
///
/// Loose inline text that sits directly inside layout containers — a very common
/// pattern in table-based newsletter HTML where body copy lives in bare
/// &lt;span&gt;/&lt;div&gt; rather than &lt;p&gt; — is captured: consecutive inline
/// content is emitted as a paragraph, mirroring how CSS wraps inline content in
/// anonymous block boxes. Sponsored sections are dropped via
/// <see cref="HtmlExtractionOptions"/>.
/// </summary>
public static partial class HtmlContentExtractor
{
    public static ExtractedContent Extract(string html, HtmlExtractionOptions? options = null)
    {
        options ??= HtmlExtractionOptions.Default;

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

        Walk(root, blocks, options);
        return new ExtractedContent { Blocks = blocks };
    }

    private static void Walk(HtmlNode node, List<ContentBlock> blocks, HtmlExtractionOptions options)
    {
        // Inline content directly inside this node is buffered and flushed as a
        // single paragraph whenever a block-level element (or the end of the
        // node) interrupts it, so document order is preserved.
        var inline = new List<HtmlNode>();

        void FlushInline()
        {
            if (inline.Count == 0)
                return;

            var runs = CollectAndMerge(inline);
            inline.Clear();

            if (runs.Count > 0)
                AddBlock(new TextBlock { Type = ContentBlockType.Paragraph, RichText = runs }, options, blocks);
        }

        foreach (var child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                case HtmlNodeType.Text:
                    if (!IsWhitespace(child.InnerText))
                        inline.Add(child);
                    continue;
                case HtmlNodeType.Element:
                    break;
                default:
                    continue; // comments, etc.
            }

            var name = child.Name.ToLowerInvariant();

            if (ShouldSkipElement(name, child, options))
                continue;

            if (IsInline(name))
            {
                inline.Add(child);
                continue;
            }

            // Block-level element: emit any buffered inline content first, then
            // handle the block itself.
            FlushInline();

            switch (name)
            {
                case "h1":
                case "h2":
                case "h3":
                    AddTextBlock(child, MapHeading(name), options, blocks);
                    break;

                // Notion only supports heading levels 1–3; map h4–h6 to h3.
                case "h4":
                case "h5":
                case "h6":
                    AddTextBlock(child, ContentBlockType.Heading3, options, blocks);
                    break;

                case "p":
                    AddTextBlock(child, ContentBlockType.Paragraph, options, blocks);
                    break;

                case "ul":
                    AddListItems(child, ContentBlockType.BulletedListItem, options, blocks);
                    break;

                case "ol":
                    AddListItems(child, ContentBlockType.NumberedListItem, options, blocks);
                    break;

                default:
                    // Container/layout element: recurse into its children.
                    Walk(child, blocks, options);
                    break;
            }
        }

        FlushInline();
    }

    private static void AddListItems(HtmlNode listNode, ContentBlockType itemType, HtmlExtractionOptions options, List<ContentBlock> blocks)
    {
        foreach (var li in listNode.ChildNodes)
        {
            if (li.NodeType != HtmlNodeType.Element ||
                !string.Equals(li.Name, "li", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsHidden(li))
                continue;

            AddTextBlock(li, itemType, options, blocks);

            // Handle nested lists by appending their items after the parent item.
            foreach (var nested in li.ChildNodes)
            {
                if (nested.NodeType != HtmlNodeType.Element) continue;
                var n = nested.Name.ToLowerInvariant();
                if (n == "ul") AddListItems(nested, ContentBlockType.BulletedListItem, options, blocks);
                else if (n == "ol") AddListItems(nested, ContentBlockType.NumberedListItem, options, blocks);
            }
        }
    }

    private static void AddTextBlock(HtmlNode node, ContentBlockType type, HtmlExtractionOptions options, List<ContentBlock> blocks)
    {
        var merged = CollectAndMerge(node);
        if (merged.Count == 0)
            return;

        AddBlock(new TextBlock { Type = type, RichText = merged }, options, blocks);
    }

    // ---- Sponsor / unwanted-content filtering -------------------------------

    private static void AddBlock(ContentBlock block, HtmlExtractionOptions options, List<ContentBlock> blocks)
    {
        if (block is TextBlock text && IsIgnored(text, options))
            return;

        blocks.Add(block);
    }
    
    private static bool IsIgnored(TextBlock block, HtmlExtractionOptions options)
    {
        if (options.IgnoreTextMarkers.Count == 0)
            return false;

        var sb = new StringBuilder();
        foreach (var run in block.RichText)
            sb.Append(run.Text);
        var text = sb.ToString();

        foreach (var marker in options.IgnoreTextMarkers)
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
    
    private static bool ShouldSkipElement(string name, HtmlNode node, HtmlExtractionOptions options)
    {
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
                return true;
        }

        if (IsHidden(node))
            return true;

        return options.IgnoreElementIdsOrClasses.Count > 0 && MatchesIgnoredIdOrClass(node, options);
    }

    private static bool MatchesIgnoredIdOrClass(HtmlNode node, HtmlExtractionOptions options)
    {
        var id = node.GetAttributeValue("id", null);
        var classAttr = node.GetAttributeValue("class", null);

        foreach (var token in options.IgnoreElementIdsOrClasses)
        {
            if (!string.IsNullOrEmpty(id) && id.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(classAttr) && HasClass(classAttr, token))
                return true;
        }

        return false;
    }

    private static bool HasClass(string classAttr, string token)
    {
        foreach (var cls in classAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (cls.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static bool IsHidden(HtmlNode node)
    {
        var style = node.GetAttributeValue("style", null);
        if (string.IsNullOrEmpty(style))
            return false;

        var compact = style.Replace(" ", string.Empty).ToLowerInvariant();
        return compact.Contains("display:none") || compact.Contains("visibility:hidden");
    }

    // ---- Rich-text collection ----------------------------------------------

    private static IReadOnlyList<RichText> CollectAndMerge(HtmlNode node)
    {
        var runs = new List<RichText>();
        foreach (var child in node.ChildNodes)
            CollectNode(child, currentHref: null, runs);
        return Finalize(runs);
    }

    private static IReadOnlyList<RichText> CollectAndMerge(List<HtmlNode> nodes)
    {
        var runs = new List<RichText>();
        foreach (var node in nodes)
            CollectNode(node, currentHref: null, runs);
        return Finalize(runs);
    }

    private static void CollectNode(HtmlNode node, string? currentHref, List<RichText> runs)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
            {
                var text = Normalize(HtmlEntity.DeEntitize(node.InnerText));
                if (text.Length > 0)
                    runs.Add(new RichText { Text = text, Href = currentHref });
                return;
            }
            case HtmlNodeType.Element:
                break;
            default:
                return;
        }

        var name = node.Name.ToLowerInvariant();

        switch (name)
        {
            // Block-level lists become their own blocks via the walker; don't
            // duplicate their text inside the surrounding rich text.
            case "ul":
            case "ol":
            case "script":
            case "style":
            case "noscript":
            case "img":
            case "svg":
            case "picture":
            case "video":
            case "audio":
            case "iframe":
                return;

            case "br":
                runs.Add(new RichText { Text = "\n", Href = currentHref });
                return;

            case "a":
            {
                var href = node.GetAttributeValue("href", null);
                var effective = string.IsNullOrWhiteSpace(href) ? currentHref : href;
                foreach (var child in node.ChildNodes)
                    CollectNode(child, effective, runs);
                return;
            }
        }

        if (IsHidden(node))
            return;

        foreach (var child in node.ChildNodes)
            CollectNode(child, currentHref, runs);
    }

    private static IReadOnlyList<RichText> Finalize(List<RichText> runs) =>
        MergeSameHref(CollapseWhitespace(runs));

    /// <summary>
    /// Collapses whitespace to single spaces across run boundaries, limits the
    /// consecutive newlines produced by &lt;br&gt; to a single blank line, and
    /// trims the block as a whole. Text-node whitespace (incl. newlines) was
    /// already normalized to spaces, so only explicit &lt;br&gt; line breaks
    /// survive as '\n' here.
    /// </summary>
    private static List<RichText> CollapseWhitespace(List<RichText> runs)
    {
        var result = new List<RichText>(runs.Count);

        var pendingSpace = false;
        var pendingNewlines = 0;
        var seenVisible = false;

        foreach (var run in runs)
        {
            var sb = new StringBuilder(run.Text.Length);

            foreach (var ch in run.Text)
            {
                if (ch == '\n')
                {
                    pendingSpace = false;
                    if (seenVisible)
                        pendingNewlines = Math.Min(pendingNewlines + 1, 2);
                    continue;
                }

                if (char.IsWhiteSpace(ch))
                {
                    if (seenVisible && pendingNewlines == 0)
                        pendingSpace = true;
                    continue;
                }

                if (pendingNewlines > 0)
                {
                    sb.Append('\n', pendingNewlines);
                    pendingNewlines = 0;
                }
                else if (pendingSpace)
                {
                    sb.Append(' ');
                }

                pendingSpace = false;
                sb.Append(ch);
                seenVisible = true;
            }

            if (sb.Length > 0)
                result.Add(run with { Text = sb.ToString() });
        }

        return result;
    }

    private static List<RichText> MergeSameHref(List<RichText> runs)
    {
        var result = new List<RichText>(runs.Count);
        foreach (var run in runs)
        {
            if (result.Count > 0 && result[^1].Href == run.Href)
                result[^1] = result[^1] with { Text = result[^1].Text + run.Text };
            else
                result.Add(run);
        }
        return result;
    }

    // ---- Image support (currently unused; images are stripped) --------------

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

    // ---- Element classification & small helpers -----------------------------

    private static bool IsInline(string name) => name switch
    {
        "a" or "span" or "strong" or "b" or "em" or "i" or "u" or "s" or "strike" or
        "small" or "big" or "sub" or "sup" or "mark" or "code" or "kbd" or "samp" or
        "var" or "abbr" or "cite" or "q" or "time" or "label" or "font" or "tt" or
        "del" or "ins" or "bdi" or "bdo" or "wbr" or "nobr" or "br" => true,
        _ => false
    };

    private static bool IsWhitespace(string text) =>
        string.IsNullOrWhiteSpace(HtmlEntity.DeEntitize(text));

    private static string Normalize(string text) =>
        WhitespaceRegex.Replace(text, " ");

    private static ContentBlockType MapHeading(string tag) => tag switch
    {
        "h1" => ContentBlockType.Heading1,
        "h2" => ContentBlockType.Heading2,
        _    => ContentBlockType.Heading3
    };

    private static readonly Regex WhitespaceRegex = MyRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
