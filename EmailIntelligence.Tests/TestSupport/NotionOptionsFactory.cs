using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EmailIntelligence.Tests.TestSupport;

public static class NotionOptionsFactory
{
    private const string TitlePropertyName = "Name";

    private static NotionOptions Create() => new()
    {
        AuthToken = "secret-token",
        DatabaseId = "db-123",
        Properties =
        [
            new NotionPropertyOptions { Name = TitlePropertyName, Type = NotionPropertyType.Title, Value = null },
            new NotionPropertyOptions { Name = "Datum", Type = NotionPropertyType.Date, Value = null },
            new NotionPropertyOptions { Name = "Front", Type = NotionPropertyType.Select, Value = null },
            new NotionPropertyOptions { Name = "Källa", Type = NotionPropertyType.Select, Value = null },
            new NotionPropertyOptions { Name = "Tanketyp", Type = NotionPropertyType.Select, Value = null },
            new NotionPropertyOptions { Name = "Status", Type = NotionPropertyType.Select, Value = "Inbox" }
        ]
    };

    public static IOptions<NotionOptions> AsOptions(NotionOptions? options = null) =>
        Options.Create(options ?? Create());
}
