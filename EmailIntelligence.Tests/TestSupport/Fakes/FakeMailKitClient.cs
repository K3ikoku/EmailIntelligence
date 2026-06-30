using EmailIntelligence.Infrastructure.Clients.Interfaces;
using MailKit;
using MimeKit;

namespace EmailIntelligence.Tests.TestSupport.Fakes;

public sealed class FakeMailKitClient(params MimeMessage[] messages) : IMailKitClient
{
    public List<string> MovedMessageIds { get; } = [];
    public int MoveCallCount { get; private set; }

    public Task<IEnumerable<MimeMessage>> GetEmails() =>
        Task.FromResult<IEnumerable<MimeMessage>>(messages);

    public Task<IEnumerable<UniqueId>> MoveToFolderAsync(IEnumerable<string> messageId)
    {
        MoveCallCount++;
        var ids = messageId.ToList();
        MovedMessageIds.AddRange(ids);

        var uids = ids.Select((_, i) => new UniqueId((uint)(i + 1)));
        return Task.FromResult<IEnumerable<UniqueId>>(uids.ToList());
    }
}
