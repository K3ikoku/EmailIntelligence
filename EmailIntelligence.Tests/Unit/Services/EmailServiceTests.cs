using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Tests.TestSupport;
using MailKit;
using MimeKit;

namespace EmailIntelligence.Tests.Unit.Services;

public class EmailServiceTests
{
    private static IMailKitClient ClientReturning(params MimeMessage[] messages)
    {
        var client = Substitute.For<IMailKitClient>();
        client.GetEmails().Returns(messages);
        return client;
    }

    [Fact]
    public async Task GetAndCleanEmails_maps_all_fields()
    {
        var received = new DateTimeOffset(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);
        var message = new MimeMessageBuilder()
            .From("TLDR", "news@tldr.tech")
            .Subject("Daily digest")
            .MessageId("abc@x")
            .ReceivedAt(received)
            .HtmlBody("<p>Hello</p>")
            .Build();

        var email = (await new EmailService(ClientReturning(message)).GetAndCleanEmails()).ShouldHaveSingleItem();

        email.EmailSender.ShouldBe("TLDR");
        email.Subject.ShouldBe("Daily digest");
        email.MessageId.ShouldBe("abc@x");
        email.DateReceived.ShouldBe(received);
        email.EmailBody.ShouldContain("Hello");
    }

    [Fact]
    public async Task GetAndCleanEmails_falls_back_to_address_when_display_name_missing()
    {
        var message = new MimeMessageBuilder().From(name: null, address: "noreply@news.io").Build();
        var email = (await new EmailService(ClientReturning(message)).GetAndCleanEmails()).ShouldHaveSingleItem();
        email.EmailSender.ShouldBe("noreply@news.io");
    }

    [Fact]
    public async Task GetAndCleanEmails_does_not_throw_when_from_header_is_absent()
    {
        // Regression guard for the previous From.First() crash on senderless mail.
        var message = new MimeMessageBuilder().WithoutFrom().Build();
        var email = (await new EmailService(ClientReturning(message)).GetAndCleanEmails()).ShouldHaveSingleItem();
        email.EmailSender.ShouldBe("Unknown sender");
    }

    [Fact]
    public async Task GetAndCleanEmails_uses_empty_body_for_plain_text_only_mail()
    {
        var message = new MimeMessageBuilder().TextBodyOnly("just text").Build();
        var email = (await new EmailService(ClientReturning(message)).GetAndCleanEmails()).ShouldHaveSingleItem();
        email.EmailBody.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetAndCleanEmails_uses_empty_subject_when_missing()
    {
        var message = new MimeMessageBuilder().Subject(null).Build();
        var email = (await new EmailService(ClientReturning(message)).GetAndCleanEmails()).ShouldHaveSingleItem();
        email.Subject.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetAndCleanEmails_returns_empty_when_no_messages()
    {
        (await new EmailService(ClientReturning()).GetAndCleanEmails()).ShouldBeEmpty();
    }

    [Fact]
    public async Task MoveProcessedEmailsAsync_forwards_ids_and_succeeds_when_messages_move()
    {
        var client = Substitute.For<IMailKitClient>();
        client.MoveToFolderAsync(Arg.Any<IEnumerable<string>>())
            .Returns([new UniqueId(1u)]);

        await new EmailService(client).MoveProcessedEmailsAsync(["m1", "m2"]);

        await client.Received(1).MoveToFolderAsync(
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "m1", "m2" })));
    }

    [Fact]
    public async Task MoveProcessedEmailsAsync_throws_when_nothing_moved()
    {
        // Characterizes current behaviour: a no-op move is treated as a failure. See review notes.
        var client = Substitute.For<IMailKitClient>();
        client.MoveToFolderAsync(Arg.Any<IEnumerable<string>>())
            .Returns(Array.Empty<UniqueId>());

        await Should.ThrowAsync<Exception>(
            () => new EmailService(client).MoveProcessedEmailsAsync(["m1"]));
    }
}
