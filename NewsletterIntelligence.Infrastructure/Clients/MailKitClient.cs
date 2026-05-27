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
        using var client = new ImapClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl);
        await client.AuthenticateAsync(settings.Username, settings.Password);
        
        // Open folder
        var emails = await client.GetFolderAsync(settings.TargetFolder);
        await emails.OpenAsync(FolderAccess.ReadOnly);
        
        // Get all messages
        var uids = await emails.SearchAsync(SearchQuery.All);

        var messages = new List<MimeMessage>();
        foreach (var uid in uids)
            messages.Add(await emails.GetMessageAsync(uid));

        await client.DisconnectAsync(true);
        return messages;
    }
}