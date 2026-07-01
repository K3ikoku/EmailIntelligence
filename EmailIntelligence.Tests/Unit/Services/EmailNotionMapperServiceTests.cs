using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Tests.TestSupport;
using Notion.Client;
using Page = EmailIntelligence.Domain.Entities.Drafts.Notion.Page;

namespace EmailIntelligence.Tests.Unit.Services;

public class EmailNotionMapperServiceTests
{
    private static readonly DateTimeOffset Received = new(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);

    private static EmailNotionMapperService CreateSut() =>
        new(NotionOptionsFactory.AsOptions());

    private static Email EmailFrom(
        string sender, string subject = "Hi", string body = "<p>Hello</p>", string messageId = "m1") =>
        new()
        {
            EmailSender = sender,
            Subject = subject,
            EmailBody = body,
            DateReceived = Received,
            MessageId = messageId
        };

    private static string SelectValue(Page draft, string propertyName) =>
        ((SelectPropertyValue)draft.Properties.Single(p => p.Name == propertyName).Value).Select.Name;

    [Fact]
    public void MapEmail_builds_title_from_date_sender_and_subject()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR", subject: "Daily digest"));

        draft.Title.ShouldBe($"{DateTimeOffset.UtcNow:yyyy-MM-dd} - TLDR - Daily digest");
    }

    [Fact]
    public void MapEmail_carries_message_id_as_email_id()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR", messageId: "abc@x"));
        draft.EmailId.ShouldBe("abc@x");
    }

    [Theory]
    [InlineData("TLDR", "It")]
    [InlineData("TLDR AI", "It")]
    [InlineData("TLDR DevOps", "It")]
    [InlineData("Världens Historia", "Vetenskap")]
    [InlineData("Illustrerad Vetenskap", "Vetenskap")]
    [InlineData("Geopolitics Daily", "Nyheter")]
    public void MapEmail_maps_known_senders_to_their_front(string sender, string expectedFront)
    {
        var draft = CreateSut().MapEmail(EmailFrom(sender));
        SelectValue(draft, "Front").ShouldBe(expectedFront);
    }

    [Theory]
    [InlineData("Some Unknown Newsletter")]
    [InlineData("")]
    public void MapEmail_routes_unknown_senders_to_unclassified(string sender)
    {
        // Regression guard: an unrecognised sender must not throw and abort the whole run.
        var draft = CreateSut().MapEmail(EmailFrom(sender));
        SelectValue(draft, "Front").ShouldBe("Oklassificerat");
    }

    [Fact]
    public void MapEmail_sets_source_column_to_the_sender()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR Crypto"));
        SelectValue(draft, "Källa").ShouldBe("TLDR Crypto");
    }

    [Fact]
    public void MapEmail_sets_constant_thought_type_column()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR"));
        SelectValue(draft, "Tanketyp").ShouldBe("Nyhetsbrev");
    }

    [Fact]
    public void MapEmail_uses_literal_value_for_configured_select()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR"));
        SelectValue(draft, "Status").ShouldBe("Inbox");
    }

    [Fact]
    public void MapEmail_maps_date_column_from_received_date()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR"));

        var date = ((DatePropertyValue)draft.Properties.Single(p => p.Name == "Datum").Value).Date;
        date.Start.ShouldBe(Received);
    }

    [Fact]
    public void MapEmail_writes_title_property_as_title_value()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR", subject: "Subj"));

        var title = (TitlePropertyValue)draft.Properties.Single(p => p.Name == "Name").Value;
        var run = title.Title.ShouldHaveSingleItem().ShouldBeOfType<RichTextText>();
        run.Text.Content.ShouldBe(draft.Title);
    }

    [Fact]
    public void MapEmail_renders_body_into_notion_blocks()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR", body: "<h1>Heading</h1><p>Body</p>"));

        draft.Blocks.Count().ShouldBe(2);
        draft.Blocks.First().ShouldBeOfType<HeadingOneBlock>();
        draft.Blocks.Last().ShouldBeOfType<ParagraphBlock>();
    }

    [Fact]
    public void MapEmail_with_empty_body_produces_no_blocks()
    {
        var draft = CreateSut().MapEmail(EmailFrom("TLDR", body: string.Empty));
        draft.Blocks.ShouldBeEmpty();
    }
}
