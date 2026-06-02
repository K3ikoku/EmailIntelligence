using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Domain.Entities;
using Notion.Client;

namespace NewsletterIntelligence.Infrastructure.Clients;

/// <summary>
/// Creates Notion pages from structured content using config-driven property mapping.
/// </summary>
public sealed class NotionApiClient(INotionClient client, IOptions<NotionOptions> options)
{

    /// <summary>
    /// Creates a new page under the configured parent, with the given title,
    /// child blocks, and a value bag keyed by the same keys as <c>Notion:Properties</c>
    /// in configuration. Returns the URL of the created page.
    /// </summary>
    public async Task<string> CreatePage(
        NotionPageDraft draft)
    {
        var parameters = PagesCreateParametersBuilder
            .Create(new ParentPageInput { PageId = options.Value.ParentPageId })
            .Build();

        Page page;
        try
        {
            page = await client.Pages.CreateAsync(parameters);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        parameters.Properties = BuildProperties(draft.Properties);
        parameters.Children = draft.Blocks.ToList();

        return page.Url;
    }

    private Dictionary<string, PropertyValue> BuildProperties(IEnumerable<NotionPageProperty> propertiesList)
    {
        var properties = propertiesList.ToList();
        var propertyDictionary = new Dictionary<string, PropertyValue>(properties.Count + 1);
        foreach (var rule in options.Value.Properties)
        {
            var property = properties.Single(p => p.Name == rule.Name);
            propertyDictionary[property.Name] = property.Value;
        }

        return propertyDictionary;
    }
}

