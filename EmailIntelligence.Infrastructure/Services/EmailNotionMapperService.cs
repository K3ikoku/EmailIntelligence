using Microsoft.Extensions.Options;
using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using EmailIntelligence.Infrastructure.Utilities;
using Notion.Client;
using Page = EmailIntelligence.Domain.Entities.Drafts.Notion.Page;
using Property = EmailIntelligence.Domain.Entities.Drafts.Notion.Property;

namespace EmailIntelligence.Infrastructure.Services;

public class EmailNotionMapperService(IOptions<NotionOptions> options) : IEmailNotionMapperService
{
    public Page MapEmail(Email email)
    {
        var title = $"{DateTimeOffset.UtcNow:yyyy-MM-dd} - {email.EmailSender} - {email.Subject}";
        var response = new Page
        {
            Title = title,
            EmailId = email.MessageId,
            Blocks = MapBlocks(email),
            Properties = MapProperties(email, title)
        };

        return response;
    }

    private static List<IBlock> MapBlocks(Email email)
    {
        var mappedEmail = HtmlContentExtractor.Extract(email.EmailBody);
        var blocks = mappedEmail.Blocks.Select(block => block.ToNotionBlock()).ToList();
        return blocks;
    }

    private List<Property> MapProperties(Email email, string title)
    {
        var result = new List<Property>
        {
            // Create title property
            new()
            {
                Name = options.Value.Properties.First().Name,
                Value = ToPropertyValue(NotionPropertyType.Title, $"{title}")
            }
        };
        foreach (var property in options.Value.Properties.Skip(1))
        {
            if (property.Value is not null)
            {
                result.Add(new Property
                {
                    Name = property.Name,
                    Value = ToPropertyValue(property.Type, property.Value)
                });
                continue;
            }

            switch (property.Type)
            {
                case NotionPropertyType.Date:
                    result.Add(new Property
                    {
                        Name = property.Name,
                        Value = ToPropertyValue(NotionPropertyType.Date, email.DateReceived)
                    });
                    continue;
                case NotionPropertyType.Select:
                    result.Add(new Property
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
                "TLDR" or
                    "TLDR Dev" or
                    "TLDR IT" or
                    "TLDR Founders" or
                    "TLDR Data" or
                    "TLDR Fintech" or
                    "TLDR DevOps" or
                    "TLDR Crypto" or
                    "TLDR Design" or
                    "TLDR AI" => nameof(Front.It),
                "Världens Historia" or "Illustrerad Vetenskap" => nameof(Front.Vetenskap),
                "Geopolitics Daily" => nameof(Front.Nyheter),
                _ => nameof(Front.Oklassificerat)
            },
            "Källa" => sender,
            "Tanketyp" => "Nyhetsbrev",
            _ => throw new ArgumentOutOfRangeException(nameof(property), $"{property.Name} is not defined in mapper.")
            
        };
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
            Date = new Date { Start = (DateTimeOffset)value }
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
        NotionPropertyType.Checkbox => new CheckboxPropertyValue { Checkbox = bool.Parse(value as string ?? "") },
        NotionPropertyType.Url => new UrlPropertyValue { Url = value.ToString()! },
        NotionPropertyType.Email => new EmailPropertyValue { Email = value.ToString()! },
        NotionPropertyType.Number => new NumberPropertyValue { Number = Convert.ToDouble(value) },
        _ => throw new NotSupportedException($"Property type {type} is not supported.")
    };
}