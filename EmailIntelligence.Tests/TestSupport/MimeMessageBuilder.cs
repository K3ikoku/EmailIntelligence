using MimeKit;

namespace EmailIntelligence.Tests.TestSupport;

public sealed class MimeMessageBuilder
{
    private string? _fromName = "TLDR";
    private string _fromAddress = "news@tldr.tech";
    private bool _includeFrom = true;
    private string? _subject = "Daily digest";
    private string? _messageId = "msg-001@tldr.tech";
    private DateTimeOffset _date = new(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);
    private string? _htmlBody = "<p>Hello</p>";
    private string? _textBody;

    public MimeMessageBuilder From(string? name, string address = "news@tldr.tech")
    {
        _fromName = name;
        _fromAddress = address;
        _includeFrom = true;
        return this;
    }

    public MimeMessageBuilder WithoutFrom()
    {
        _includeFrom = false;
        return this;
    }

    public MimeMessageBuilder Subject(string? subject)
    {
        _subject = subject;
        return this;
    }

    public MimeMessageBuilder MessageId(string? messageId)
    {
        _messageId = messageId;
        return this;
    }

    public MimeMessageBuilder ReceivedAt(DateTimeOffset date)
    {
        _date = date;
        return this;
    }

    public MimeMessageBuilder HtmlBody(string? html)
    {
        _htmlBody = html;
        return this;
    }

    public MimeMessageBuilder TextBodyOnly(string text)
    {
        _htmlBody = null;
        _textBody = text;
        return this;
    }

    public MimeMessage Build()
    {
        var message = new MimeMessage { Date = _date };

        if (_includeFrom)
            message.From.Add(new MailboxAddress(_fromName, _fromAddress));

        if (_subject is not null)
            message.Subject = _subject;

        if (_messageId is not null)
            message.MessageId = _messageId;

        var body = new BodyBuilder();
        if (_htmlBody is not null)
            body.HtmlBody = _htmlBody;
        if (_textBody is not null)
            body.TextBody = _textBody;

        message.Body = body.ToMessageBody();
        return message;
    }
}
