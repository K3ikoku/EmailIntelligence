using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Tests.TestSupport;
using EmailIntelligence.Tests.TestSupport.Fakes;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Notion.Client;

namespace EmailIntelligence.Tests.Integration;

[Trait("Category", "Integration")]
public class NewsletterPipelineIntegrationTests
{
    private static string Today => $"{DateTimeOffset.UtcNow:yyyy-MM-dd}";

    private static NewsletterPipelineService BuildPipeline(
        FakeMailKitClient mail, RecordingNotionApiClient notion)
    {
        var emailService = new EmailService(mail);
        var mapper = new EmailNotionMapperService(NotionOptionsFactory.AsOptions());
        var notionService = new NotionService(notion, NullLogger<NotionService>.Instance);

        var telemetry = new TelemetryClient(new TelemetryConfiguration
        {
            TelemetryChannel = new StubTelemetryChannel(),
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        });

        return new NewsletterPipelineService(
            emailService, mapper, notionService, telemetry,
            NullLogger<NewsletterPipelineService>.Instance);
    }

    private static MimeMessage Message(string sender, string subject, string messageId, string html) =>
        new MimeMessageBuilder()
            .From(sender)
            .Subject(subject)
            .MessageId(messageId)
            .HtmlBody(html)
            .Build();

    [Fact]
    public async Task Full_run_creates_notion_pages_and_moves_every_processed_email()
    {
        var mail = new FakeMailKitClient(
            Message("TLDR", "Dev news", "m1@x", "<h1>Title</h1><p>Body with <a href=\"https://x.com\">link</a></p>"),
            Message("Världens Historia", "History", "m2@x", "<p>Något kul</p>"));
        var notion = new RecordingNotionApiClient();

        var result = await BuildPipeline(mail, notion).ProcessEmails();

        result.ShouldBeTrue();

        notion.CreatedDrafts.Select(d => d.Title).ShouldBe(
        [
            $"{Today} - TLDR - Dev news",
            $"{Today} - Världens Historia - History"
        ]);

        mail.MoveCallCount.ShouldBe(1);
        mail.MovedMessageIds.ShouldBe(["m1@x", "m2@x"]);
    }

    [Fact]
    public async Task Full_run_renders_html_into_structured_notion_blocks()
    {
        var mail = new FakeMailKitClient(
            Message("TLDR", "Dev news", "m1@x", "<h1>Title</h1><p>Body</p>"));
        var notion = new RecordingNotionApiClient();

        await BuildPipeline(mail, notion).ProcessEmails();

        var draft = notion.CreatedDrafts.ShouldHaveSingleItem();
        draft.Blocks.OfType<HeadingOneBlock>().ShouldHaveSingleItem();
        draft.Blocks.OfType<ParagraphBlock>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Full_run_strips_sponsor_content_end_to_end()
    {
        var mail = new FakeMailKitClient(
            Message("TLDR", "Dev news", "m1@x", "<p>(Sponsor) buy now</p><p>real content</p>"));
        var notion = new RecordingNotionApiClient();

        await BuildPipeline(mail, notion).ProcessEmails();

        var paragraphs = notion.CreatedDrafts.ShouldHaveSingleItem()
            .Blocks.OfType<ParagraphBlock>().ToList();

        var run = paragraphs.ShouldHaveSingleItem().Paragraph.RichText
            .ShouldHaveSingleItem().ShouldBeOfType<RichTextText>();
        run.Text.Content.ShouldBe("real content");
    }

    [Fact]
    public async Task Already_existing_pages_are_not_recreated_but_are_still_moved()
    {
        var existingTitle = $"{Today} - Världens Historia - History";
        var mail = new FakeMailKitClient(
            Message("TLDR", "Dev news", "m1@x", "<p>a</p>"),
            Message("Världens Historia", "History", "m2@x", "<p>b</p>"));
        var notion = new RecordingNotionApiClient(existingTitles: [existingTitle]);

        await BuildPipeline(mail, notion).ProcessEmails();

        // Only the non-duplicate page is created...
        notion.CreatedDrafts.Select(d => d.EmailId).ShouldBe(["m1@x"]);
        // ...but both emails are considered processed and moved out of the inbox.
        mail.MovedMessageIds.ShouldBe(["m1@x", "m2@x"]);
    }

    [Fact]
    public async Task Unknown_sender_does_not_break_the_run()
    {
        var mail = new FakeMailKitClient(
            Message("Totally New Newsletter", "Hi", "m9@x", "<p>content</p>"));
        var notion = new RecordingNotionApiClient();

        var result = await BuildPipeline(mail, notion).ProcessEmails();

        result.ShouldBeTrue();
        var draft = notion.CreatedDrafts.ShouldHaveSingleItem();
        ((SelectPropertyValue)draft.Properties.Single(p => p.Name == "Front").Value)
            .Select.Name.ShouldBe("Oklassificerat");
    }
}
