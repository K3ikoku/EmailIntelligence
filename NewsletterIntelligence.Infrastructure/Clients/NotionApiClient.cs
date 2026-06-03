using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Domain.Entities;
using Notion.Client;

namespace NewsletterIntelligence.Infrastructure.Clients;

public sealed class NotionApiClient(INotionClient client, IOptions<NotionOptions> options)
{

    public async Task<string> CreatePage(NotionPageDraft draft)
    {
        var parameters = PagesCreateParametersBuilder
            .Create(new DatabaseParentInput() { DatabaseId = options.Value.DatabaseId })
            .Build();

        parameters.Properties = BuildProperties(draft.Properties);
        parameters.Children = draft.Blocks.ToList();
        var page = await client.Pages.CreateAsync(parameters);

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

