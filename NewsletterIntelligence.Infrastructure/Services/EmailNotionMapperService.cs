using Microsoft.Extensions.Options;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Domain.Entities;
using NewsletterIntelligence.Domain.Enums;
using NewsletterIntelligence.Infrastructure.Services.Interfaces;
using Notion.Client;

namespace NewsletterIntelligence.Infrastructure.Services;

public class EmailNotionMapperService(IOptions<NotionOptions> options) : IEmailNotionMapperService
{
    public async Task<NotionPageDraft> MapEmail(Email email)
    {
        var response = new NotionPageDraft
        {
            Blocks = null,
            Properties = await MapProperties(email) 
        };

        return response;
    }
    
    private async Task<IEnumerable<NotionPageProperty>> MapProperties(Email email)
    {
        var result = new List<NotionPageProperty>
        {
            // Create title property
            new()
            {
                Name = options.Value.Properties.First().Name,
                Value = ToPropertyValue(NotionPropertyType.Title, $"{email.EmailSender} - {email.Subject} - {DateTimeOffset.UtcNow:yyyy-MM-dd}")
            }
        };
        foreach (var property in options.Value.Properties.Skip(1))
        {
            if (property.Value is not null)
            {
                result.Add(new NotionPageProperty
                {
                    Name = property.Name,
                    Value = ToPropertyValue(property.Type, property.Value)
                });
                continue;
            }

            switch (property.Type)
            {
                case NotionPropertyType.Date:
                    result.Add(new NotionPageProperty
                    {
                        Name = property.Name,
                        Value = ToPropertyValue(NotionPropertyType.Date, email.DateReceived)
                    });
                    continue;
                case NotionPropertyType.Select:
                    result.Add(new NotionPageProperty
                    {
                        Name = property.Name,
                        Value = new SelectPropertyValue
                        {
                            Select = new SelectOption { Name = ToSelectPropertyValue(property, email.EmailSender) }
                        }
                    });
                    continue;
                case NotionPropertyType.Checkbox:
                case NotionPropertyType.Text:
                case NotionPropertyType.MultiSelect:
                case NotionPropertyType.Url:
                case NotionPropertyType.Email:
                case NotionPropertyType.Number:
                case NotionPropertyType.Title:
                default:
                    throw new ArgumentOutOfRangeException(nameof(property.Type), $"{property.Type} is not supported.");
            }
        }
        
        return result;
    }

    private static string ToSelectPropertyValue(NotionPropertyOptions property, string sender)
    {
        return property.Name switch
        {
            "Front" => sender switch
            {
                "TLDR IT" => nameof(Front.It),
                _ => throw new ArgumentOutOfRangeException(nameof(sender), $"{sender} is not defined in mapper.")
            },
            "Källa" => sender,
            _ => throw new ArgumentOutOfRangeException(nameof(property), $"{property.Name} is not defined in mapper.")
        };
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