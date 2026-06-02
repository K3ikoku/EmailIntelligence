using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Domain.Entities;
using NewsletterIntelligence.Domain.Enums;
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

        parameters.Properties = BuildProperties(draft.Properties);
        parameters.Children = draft.Blocks.ToList();

        var page = await client.Pages.CreateAsync(parameters);
        return page.Url;
    }

    private Dictionary<string, PropertyValue> BuildProperties(IEnumerable<NotionPageProperty> propertiesList)
    {
        var properties = propertiesList.ToList();
        var propertyDictionary = new Dictionary<string, PropertyValue>(properties.Count + 1);
        foreach (var property in options.Value.Properties)
        {
            propertyDictionary[property.Name] = ToPropertyValue(property.Type, property.Value!);
        }

        return propertyDictionary;
    }

    private static PropertyValue ToPropertyValue(NotionPropertyType type, object value) => type switch
    {
        NotionPropertyType.Title => new TitlePropertyValue
        {
            Title = [new RichTextText { Text = new Text { Content = value.ToString()! } }]
        },
        NotionPropertyType.Text => new RichTextPropertyValue
        {
            RichText = [new RichTextText { Text = new Text { Content = value.ToString()! } }]
        },
        NotionPropertyType.Date => new DatePropertyValue
        {
            Date = new Date { Start = Convert.ToDateTime(value) }
        },
        NotionPropertyType.Select => new SelectPropertyValue
        {
            
            Select = new SelectOption { Name = value.ToString()! }
        },
        NotionPropertyType.MultiSelect => new MultiSelectPropertyValue
        {
            MultiSelect = ((IEnumerable<string>)value)
                .Select(o => new SelectOption { Name = o })
                .ToList()
        },
        NotionPropertyType.Checkbox => new CheckboxPropertyValue { Checkbox = (bool)value },
        NotionPropertyType.Url => new UrlPropertyValue { Url = value.ToString()! },
        NotionPropertyType.Email => new EmailPropertyValue { Email = value.ToString()! },
        NotionPropertyType.Number => new NumberPropertyValue { Number = Convert.ToDouble(value) },
        _ => throw new NotSupportedException($"Property type {type} is not supported.")
    };
}

