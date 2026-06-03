using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Infrastructure.Clients.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Clients;

public class MailKitClient : IMailKitClient
{
    private readonly ImapClient _client;
    private readonly ImapSettings _settings;
    public MailKitClient(ImapSettings settings)
    {
        _settings = settings;
        _client = new ImapClient();
        _client.Connect(settings.Host, settings.Port, settings.UseSsl);
        _client.Authenticate(settings.Username, settings.Password);
    }
    
    public async Task<IEnumerable<MimeMessage>> GetEmails()
    {
        var folder = await _client.GetFolderAsync(_settings.RetrievingFolder);
        await folder.OpenAsync(FolderAccess.ReadOnly);

        var uids = await folder.SearchAsync(SearchQuery.All);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
            messages.Add(await folder.GetMessageAsync(uid));

        await _client.DisconnectAsync(true);
        return messages;
    }

    public async Task<IReadOnlyList<UniqueId>> MoveToFolderAsync(IEnumerable<string> messageIds)
    {
        var source = await _client.GetFolderAsync(_settings.RetrievingFolder);
        await source.OpenAsync(FolderAccess.ReadWrite);

        var uniqueIds = messageIds.Select(UniqueId.Parse).ToList();
        var existingSummaries =
            await source.FetchAsync(uniqueIds, MessageSummaryItems.UniqueId);
        var ids = existingSummaries.Select(s => s.UniqueId).ToList();

        IMailFolder destination;
        try
        {
            destination = await _client.GetFolderAsync(_settings.ProcessedFolder);
        }
        catch (FolderNotFoundException)
        {
            destination = await _client.GetFolder(_client.PersonalNamespaces[0])
                .CreateAsync(_settings.ProcessedFolder, true);
        }

        await source.MoveToAsync(ids, destination);

        await _client.DisconnectAsync(true);
        return (ids);
    }
}