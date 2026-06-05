using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Infrastructure.Clients.Interfaces;

namespace NewsletterIntelligence.Infrastructure.Clients;

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
        var client = new ImapClient();
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

        var query = messageIds.Select(x => (SearchQuery)SearchQuery.HeaderContains("Message-Id", x))
            .Aggregate((current, next) => current.Or(next));
        var uids = await sourceFolder.SearchAsync(query);

        foreach (var uid in uids)
            await sourceFolder.MoveToAsync(uid, destinationFolder);

        await client.DisconnectAsync(true);
        return uids;
    }
}