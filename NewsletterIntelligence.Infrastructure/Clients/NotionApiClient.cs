using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
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
    public async Task<string> CreatePageAsync(
        string title,
        IEnumerable<IBlock> blocks,
        Dictionary<string, object> properties)
    {
        var parameters = PagesCreateParametersBuilder
            .Create(new ParentPageInput { PageId = options.Value.ParentPageId })
            .Build();

        parameters.Properties = BuildProperties(title, properties);
        parameters.Children = blocks.ToList();

        var page = await client.Pages.CreateAsync(parameters);
        return page.Url;
    }

    private Dictionary<string, PropertyValue> BuildProperties(
        string title,
        Dictionary<string, object> values)
    {
        var result = new Dictionary<string, PropertyValue>(values.Count + 1);

        // Title is always handled separately and never read from `values`.
        var titleMapping = options.Value.Properties.Values.FirstOrDefault(p => p.Type == NotionPropertyType.Title)
                           ?? throw new InvalidOperationException("No property of type Title is configured.");

        result[titleMapping.Name] = ToPropertyValue(NotionPropertyType.Title, title);

        foreach (var (key, value) in values)
        {
            if (!options.Value.Properties.TryGetValue(key, out var mapping))
                throw new InvalidOperationException($"No Notion property mapping configured for key '{key}'.");

            if (mapping.Type == NotionPropertyType.Title)
                continue; // title is set above

            result[mapping.Name] = ToPropertyValue(mapping.Type, value);
        }

        return result;
    }

    private static PropertyValue ToPropertyValue(NotionPropertyType type, object value) => type switch
    {
        NotionPropertyType.Title or
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

