using EmailIntelligence.Domain.Entities.Content;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Infrastructure.Utilities;

namespace EmailIntelligence.Tests.Unit.Utilities;

public class HtmlContentExtractorTests
{
    private static string Text(ContentBlock block) =>
        string.Concat(((TextBlock)block).RichText.Select(r => r.Text));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t  ")]
    public void Extract_blank_input_yields_no_blocks(string html)
    {
        HtmlContentExtractor.Extract(html).Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Extract_simple_paragraph_yields_single_paragraph_block()
    {
        var blocks = HtmlContentExtractor.Extract("<p>Hello world</p>").Blocks;

        var block = blocks.ShouldHaveSingleItem().ShouldBeOfType<TextBlock>();
        block.Type.ShouldBe(ContentBlockType.Paragraph);
        Text(block).ShouldBe("Hello world");
    }

    [Fact]
    public void Extract_maps_heading_levels_one_to_three()
    {
        var blocks = HtmlContentExtractor.Extract("<h1>A</h1><h2>B</h2><h3>C</h3>").Blocks;

        blocks.Select(b => ((TextBlock)b).Type).ShouldBe(
        [
            ContentBlockType.Heading1,
            ContentBlockType.Heading2,
            ContentBlockType.Heading3
        ]);
    }

    [Theory]
    [InlineData("<h4>x</h4>")]
    [InlineData("<h5>x</h5>")]
    [InlineData("<h6>x</h6>")]
    public void Extract_clamps_deep_headings_to_heading_three(string html)
    {
        var block = HtmlContentExtractor.Extract(html).Blocks.ShouldHaveSingleItem();
        ((TextBlock)block).Type.ShouldBe(ContentBlockType.Heading3);
    }

    [Fact]
    public void Extract_collapses_runs_of_whitespace_to_a_single_space()
    {
        var block = HtmlContentExtractor.Extract("<p>Hello     world</p>").Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("Hello world");
    }

    [Fact]
    public void Extract_trims_leading_and_trailing_whitespace()
    {
        var block = HtmlContentExtractor.Extract("<p>   Hello   </p>").Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("Hello");
    }

    [Fact]
    public void Extract_converts_br_to_newline()
    {
        var block = HtmlContentExtractor.Extract("<p>line1<br>line2</p>").Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("line1\nline2");
    }

    [Fact]
    public void Extract_caps_consecutive_newlines_at_two()
    {
        var block = HtmlContentExtractor.Extract("<p>a<br><br><br>b</p>").Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("a\n\nb");
    }

    [Fact]
    public void Extract_captures_link_href_on_the_anchored_run_only()
    {
        var block = (TextBlock)HtmlContentExtractor
            .Extract("<p>See <a href=\"https://x.com\">here</a> now</p>").Blocks.ShouldHaveSingleItem();

        Text(block).Trim().ShouldBe("See here now");

        var linked = block.RichText.Where(r => r.Href is not null).ShouldHaveSingleItem();
        linked.Href.ShouldBe("https://x.com");
        linked.Text.Trim().ShouldBe("here");
    }

    [Fact]
    public void Extract_maps_unordered_list_to_bulleted_items()
    {
        var blocks = HtmlContentExtractor.Extract("<ul><li>one</li><li>two</li></ul>").Blocks;

        blocks.Count.ShouldBe(2);
        blocks.ShouldAllBe(b => ((TextBlock)b).Type == ContentBlockType.BulletedListItem);
        blocks.Select(Text).ShouldBe(["one", "two"]);
    }

    [Fact]
    public void Extract_maps_ordered_list_to_numbered_items()
    {
        var blocks = HtmlContentExtractor.Extract("<ol><li>one</li><li>two</li></ol>").Blocks;

        blocks.ShouldAllBe(b => ((TextBlock)b).Type == ContentBlockType.NumberedListItem);
        blocks.Select(Text).ShouldBe(["one", "two"]);
    }

    [Fact]
    public void Extract_flattens_nested_lists_after_their_parent_item()
    {
        var blocks = HtmlContentExtractor.Extract("<ul><li>a<ul><li>b</li></ul></li></ul>").Blocks;

        blocks.Select(Text).ShouldBe(["a", "b"]);
    }

    [Fact]
    public void Extract_emits_image_with_url_and_alt_text()
    {
        var blocks = HtmlContentExtractor.Extract("<img src=\"https://img/x.png\" alt=\"pic\">").Blocks;

        var image = blocks.ShouldHaveSingleItem().ShouldBeOfType<ImageBlock>();
        image.Url.ShouldBe("https://img/x.png");
        image.AltText.ShouldBe("pic");
    }

    [Fact]
    public void Extract_falls_back_to_data_src_for_lazy_loaded_images()
    {
        var blocks = HtmlContentExtractor.Extract("<img data-src=\"https://img/y.png\">").Blocks;

        blocks.ShouldHaveSingleItem().ShouldBeOfType<ImageBlock>().Url.ShouldBe("https://img/y.png");
    }

    [Fact]
    public void Extract_skips_one_by_one_tracking_pixels()
    {
        HtmlContentExtractor.Extract("<img src=\"https://t/p.gif\" width=\"1\" height=\"1\">")
            .Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Extract_skips_inline_data_uri_images()
    {
        HtmlContentExtractor.Extract("<img src=\"data:image/png;base64,AAAA\">").Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Extract_emits_linked_image_as_its_own_block()
    {
        var blocks = HtmlContentExtractor
            .Extract("<a href=\"https://x.com\"><img src=\"https://img/z.png\"></a>").Blocks;

        blocks.ShouldHaveSingleItem().ShouldBeOfType<ImageBlock>().Url.ShouldBe("https://img/z.png");
    }

    [Theory]
    [InlineData("<p style=\"display:none\">secret</p>")]
    [InlineData("<p style=\"visibility:hidden\">secret</p>")]
    [InlineData("<p style=\"display: none\">secret</p>")]
    public void Extract_skips_hidden_elements(string html)
    {
        HtmlContentExtractor.Extract(html).Blocks.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("<p>ok</p><script>bad()</script>")]
    [InlineData("<p>ok</p><style>.a{}</style>")]
    [InlineData("<p>ok</p><svg><path/></svg>")]
    public void Extract_skips_non_content_elements(string html)
    {
        var block = HtmlContentExtractor.Extract(html).Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("ok");
    }

    [Fact]
    public void Extract_drops_blocks_containing_an_ignored_text_marker()
    {
        HtmlContentExtractor.Extract("<p>This is (Sponsor) content</p>").Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Extract_skips_elements_matching_an_ignored_class()
    {
        HtmlContentExtractor.Extract("<div class=\"together-with\"><p>ad copy</p></div>").Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Extract_honours_custom_ignore_options()
    {
        var options = new HtmlExtractionOptions
        {
            IgnoreTextMarkers = ["PROMO"],
            IgnoreElementIdsOrClasses = ["banner"]
        };

        var blocks = HtmlContentExtractor.Extract(
            "<p>keep</p><p>PROMO drop</p><div id=\"banner\"><p>drop too</p></div>", options).Blocks;

        blocks.Select(Text).ShouldBe(["keep"]);
    }

    [Fact]
    public void Extract_preserves_document_order_across_block_types()
    {
        var blocks = HtmlContentExtractor
            .Extract("<p>first</p><img src=\"https://i/a.png\"><h2>mid</h2><p>second</p>").Blocks;

        blocks.Count.ShouldBe(4);
        blocks[0].ShouldBeOfType<TextBlock>().Type.ShouldBe(ContentBlockType.Paragraph);
        blocks[1].ShouldBeOfType<ImageBlock>();
        blocks[2].ShouldBeOfType<TextBlock>().Type.ShouldBe(ContentBlockType.Heading2);
        blocks[3].ShouldBeOfType<TextBlock>().Type.ShouldBe(ContentBlockType.Paragraph);
    }

    [Fact]
    public void Extract_reads_body_when_full_document_is_provided()
    {
        const string html = "<html><head><title>t</title></head><body><p>body text</p></body></html>";

        var block = HtmlContentExtractor.Extract(html).Blocks.ShouldHaveSingleItem();
        Text(block).ShouldBe("body text");
    }
}
