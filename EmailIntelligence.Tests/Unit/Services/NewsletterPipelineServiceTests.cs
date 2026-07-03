using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Entities.Drafts.Notion;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using EmailIntelligence.Tests.TestSupport;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailIntelligence.Tests.Unit.Services;

public class NewsletterPipelineServiceTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailNotionMapperService _mapper = Substitute.For<IEmailNotionMapperService>();
    private readonly INotionService _notionService = Substitute.For<INotionService>();
    private readonly StubTelemetryChannel _channel = new();

    private NewsletterPipelineService CreateSut()
    {
        var config = new TelemetryConfiguration
        {
            TelemetryChannel = _channel,
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        };

        return new NewsletterPipelineService(
            _emailService, _mapper, _notionService,
            new TelemetryClient(config), NullLogger<NewsletterPipelineService>.Instance);
    }

    private static Email Email(string messageId) => new()
    {
        EmailSender = "TLDR",
        Subject = "Subject",
        EmailBody = "<p>x</p>",
        DateReceived = DateTimeOffset.UtcNow,
        MessageId = messageId
    };

    private static Page Draft(string emailId) =>
        new() { EmailId = emailId, Title = $"T-{emailId}", Blocks = [], Properties = [] };

    [Fact]
    public async Task ProcessEmails_runs_full_pipeline_and_moves_processed_mail()
    {
        var e1 = Email("e1");
        var e2 = Email("e2");
        _emailService.GetAndCleanEmails().Returns([e1, e2]);
        _mapper.MapEmail(e1).Returns(Draft("e1"));
        _mapper.MapEmail(e2).Returns(Draft("e2"));
        _notionService.CreatePage(Arg.Any<IEnumerable<Page>>()).Returns(["e1", "e2"]);

        var result = await CreateSut().ProcessEmails();

        result.ShouldBeTrue();
        await _notionService.Received(1).CreatePage(
            Arg.Is<IEnumerable<Page>>(d => d.Count() == 2));
        await _emailService.Received(1).MoveProcessedEmailsAsync(
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "e1", "e2" })));
    }

    [Fact]
    public async Task ProcessEmails_does_not_move_when_nothing_was_processed()
    {
        _emailService.GetAndCleanEmails().Returns([Email("e1")]);
        _mapper.MapEmail(Arg.Any<Email>()).Returns(Draft("e1"));
        _notionService.CreatePage(Arg.Any<IEnumerable<Page>>()).Returns([]);

        var result = await CreateSut().ProcessEmails();

        result.ShouldBeTrue();
        await _emailService.DidNotReceive().MoveProcessedEmailsAsync(Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task ProcessEmails_tracks_a_success_event()
    {
        _emailService.GetAndCleanEmails().Returns([]);
        _notionService.CreatePage(Arg.Any<IEnumerable<Page>>()).Returns([]);

        await CreateSut().ProcessEmails();

        _channel.Sent.OfType<EventTelemetry>()
            .ShouldContain(e => e.Name == "NewsletterRunCompleted" && e.Properties["Outcome"] == "Success");
    }

    [Fact]
    public async Task ProcessEmails_rethrows_and_records_failure_when_a_step_throws()
    {
        _emailService.GetAndCleanEmails()
            .Returns(Task.FromException<IEnumerable<Email>>(new InvalidOperationException("boom")));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateSut().ProcessEmails());

        _channel.Sent.OfType<ExceptionTelemetry>()
            .ShouldContain(e => e.Properties["Outcome"] == "Failed");
        _channel.Sent.OfType<EventTelemetry>()
            .ShouldContain(e => e.Name == "NewsletterRunCompleted" && e.Properties["Outcome"] == "Failed");
    }

    [Fact]
    public async Task ProcessEmails_does_not_create_pages_when_there_are_no_emails()
    {
        _emailService.GetAndCleanEmails().Returns([]);
        _notionService.CreatePage(Arg.Any<IEnumerable<Page>>()).Returns([]);

        await CreateSut().ProcessEmails();

        _mapper.DidNotReceive().MapEmail(Arg.Any<Email>());
    }
}
