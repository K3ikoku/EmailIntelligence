using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Domain.Entities;
using NewsletterIntelligence.Domain.Enums;
using NewsletterIntelligence.Infrastructure.Clients.Interfaces;
using Notion.Client;

namespace NewsletterIntelligence.Infrastructure.Clients;

public sealed class NotionApiClient(INotionClient client, IOptions<NotionOptions> options) : INotionApiClient
{
    public async Task<bool> PageExists(string title)
    {
        var response = await client.Databases.QueryAsync(
            options.Value.DatabaseId,
            new DatabasesQueryParameters
            {
                Filter = new TitleFilter(TitlePropertyName, equal: title),
                PageSize = 1
            });

        return response.Results.Count != 0;
    }

    private string TitlePropertyName =>
        (options.Value.Properties.FirstOrDefault(p => p.Type == NotionPropertyType.Title)
         ?? options.Value.Properties.First()).Name;
    
    public async Task<string?> CreatePage(NotionPageDraft draft)
    {
        var parameters = PagesCreateParametersBuilder
            .Create(new DatabaseParentInput() { DatabaseId = options.Value.DatabaseId })
            .Build();

        parameters.Properties = BuildProperties(draft.Properties);
        parameters.Children = draft.Blocks.ToList();
        var page = await client.Pages.CreateAsync(parameters);

        return page.Url != null ? draft.EmailId : null;
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

