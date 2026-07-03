using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Infrastructure.Clients;

public class MailKitClient(ImapSettings settings, ILogger<MailKitClient> logger) : IMailKitClient
{
    public async Task<IEnumerable<MimeMessage>> GetEmails()
    {
        using var client = await ConnectAsync();

        var folder = await client.GetFolderAsync(settings.RetrievingFolder);
        await folder.OpenAsync(FolderAccess.ReadOnly);

        var uids = await folder.SearchAsync(SearchQuery.All);

        var messages = new List<MimeMessage>(uids.Count);
        foreach (var uid in uids)
            messages.Add(await folder.GetMessageAsync(uid));

        logger.LogInformation("Fetched {MessageCount} message(s) from IMAP folder '{Folder}'.",
            messages.Count, settings.RetrievingFolder);

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

        using var client = await ConnectAsync();

        IMailFolder destinationFolder;
        try
        {
            destinationFolder = await client.GetFolderAsync(settings.ProcessedFolder);
        }
        catch (FolderNotFoundException)
        {
            logger.LogInformation("IMAP folder '{Folder}' does not exist; creating it.", settings.ProcessedFolder);
            destinationFolder = await client.GetFolder(client.PersonalNamespaces[0])
                    .CreateAsync(settings.ProcessedFolder, true)
                ?? throw new InvalidOperationException(
                    $"Failed to create IMAP folder '{settings.ProcessedFolder}'.");
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

        if (uids.Count < wanted.Count)
            logger.LogWarning(
                "Moved {MovedCount} of {RequestedCount} requested message(s) to '{Folder}'; "
                + "the rest were not found in '{SourceFolder}'.",
                uids.Count, wanted.Count, settings.ProcessedFolder, settings.RetrievingFolder);
        else
            logger.LogInformation("Moved {MovedCount} message(s) to '{Folder}'.",
                uids.Count, settings.ProcessedFolder);

        await client.DisconnectAsync(true);
        return uids;
    }

    private async Task<ImapClient> ConnectAsync()
    {
        var client = new ImapClient();
        try
        {
            logger.LogInformation("Connecting to IMAP server {Host}:{Port} (SSL: {UseSsl}).",
                settings.Host, settings.Port, settings.UseSsl);
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl);
            await client.AuthenticateAsync(settings.Username, settings.Password);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static string NormalizeMessageId(string messageId) =>
        messageId.Trim().Trim('<', '>');
}
