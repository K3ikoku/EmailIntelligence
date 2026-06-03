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

    public async Task<IEnumerable<UniqueId>> MoveToFolderAsync(string messageId)
    {
        var client = new ImapClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl);
        await client.AuthenticateAsync(settings.Username, settings.Password);
        
        IMailFolder destination;
        try
        {
            destination = await client.GetFolderAsync(settings.ProcessedFolder);
        }
        catch (FolderNotFoundException)
        {
            destination = await client.GetFolder(client.PersonalNamespaces[0])
                .CreateAsync(settings.ProcessedFolder, true);
        }
        var source = await client.GetFolderAsync(settings.RetrievingFolder);
        var uids = await source.SearchAsync(SearchQuery.Uids());

        foreach (var uid in uids)
        {
            await source.MoveToAsync(uid, destination);
        }

        await source.OpenAsync(FolderAccess.ReadWrite);
        await source.MoveToAsync(uids, destination);
        await client.DisconnectAsync(true);
        return uids;
    }
}