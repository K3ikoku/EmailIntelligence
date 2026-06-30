using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Infrastructure.Clients.Interfaces;

namespace EmailIntelligence.Infrastructure.Clients;

public class MailKitClient(ImapSettings settings) : IMailKitClient
{
    public async Task<IEnumerable<MimeMessage>> GetEmails()
    {
        var client = new ImapClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl);
        await client.AuthenticateAsync(settings.Username, settings.Password);
        
        var folder = await client.GetFolderAsync(settings.RetrievingFolder);
        await folder.OpenAsync(FolderAccess.ReadOnly);

        var uids = await folder.SearchAsync(SearchQuery.All);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
            messages.Add(await folder.GetMessageAsync(uid));

        await client.DisconnectAsync(true);
        return messages;
    }

    public async Task<IEnumerable<UniqueId>> MoveToFolderAsync(IEnumerable<string> messageIds)
    {
        var wanted = messageIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeMessageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (wanted.Count == 0)
            return [];

        using var client = new ImapClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl);
        await client.AuthenticateAsync(settings.Username, settings.Password);

        IMailFolder destinationFolder;
        try
        {
            destinationFolder = await client.GetFolderAsync(settings.ProcessedFolder);
        }
        catch (FolderNotFoundException)
        {
            destinationFolder = await client.GetFolder(client.PersonalNamespaces[0])
                .CreateAsync(settings.ProcessedFolder, true);
        }

        var sourceFolder = await client.GetFolderAsync(settings.RetrievingFolder);
        await sourceFolder.OpenAsync(FolderAccess.ReadWrite);
        
        var summaries = await sourceFolder.FetchAsync(
            0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope);

        var uids = summaries
            .Where(s => s.Envelope?.MessageId is { } id && wanted.Contains(NormalizeMessageId(id)))
            .Select(s => s.UniqueId)
            .ToList();

        if (uids.Count > 0)
            await sourceFolder.MoveToAsync(uids, destinationFolder);

        await client.DisconnectAsync(true);
        return uids;
    }

    private static string NormalizeMessageId(string messageId) =>
        messageId.Trim().Trim('<', '>');
}