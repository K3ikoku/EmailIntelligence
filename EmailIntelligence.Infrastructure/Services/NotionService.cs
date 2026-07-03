using EmailIntelligence.Domain.Entities.Drafts.Notion;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmailIntelligence.Infrastructure.Services;

public class NotionService(INotionApiClient notionApiClient, ILogger<NotionService> logger) : INotionService
{
    public async Task<IEnumerable<string>> CreatePage(IEnumerable<Page> drafts)
    {
        var createdIds = new List<string>();
        foreach (var draft in drafts)
        {
            // Skip drafts that are already in the Notion database.
            if (await notionApiClient.PageExists(draft.Title))
            {
                logger.LogInformation("Notion page '{Title}' already exists; skipping creation.", draft.Title);
                createdIds.Add(draft.EmailId);
                continue;
            }

            var created = await notionApiClient.CreatePage(draft);
            if (created is not null)
            {
                createdIds.Add(created);
            }
            else
            {
                logger.LogWarning(
                    "Notion returned no URL for page '{Title}'; its email will not be marked as processed.",
                    draft.Title);
            }
        }

        return createdIds;
    }
}
