using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Enums;
using EmailIntelligence.Infrastructure.Clients;
using Microsoft.Extensions.Options;
using Notion.Client;

namespace EmailIntelligence.Tests.Unit.Clients;

public class NotionApiClientTests
{
    private readonly INotionClient _client = Substitute.For<INotionClient>();
    private readonly IDatabasesClient _databases = Substitute.For<IDatabasesClient>();
    private readonly IPagesClient _pages = Substitute.For<IPagesClient>();

    public NotionApiClientTests()
    {
        _client.Databases.Returns(_databases);
        _client.Pages.Returns(_pages);
    }

    private static IOptions<NotionOptions> Options() => Microsoft.Extensions.Options.Options.Create(
        new NotionOptions
        {
            AuthToken = "t",
            DatabaseId = "db",
            Properties = [new NotionPropertyOptions { Name = "Name", Type = NotionPropertyType.Title, Value = null }]
        });

    private NotionApiClient CreateSut() => new(_client, Options());

    private static NotionPageDraft DraftWithTitleProperty() => new()
    {
        EmailId = "e1",
        Title = "T",
        Blocks = [],
        Properties =
        [
            new NotionPageProperty
            {
                Name = "Name",
                Value = new TitlePropertyValue { Title = [new RichTextText { Text = new Text { Content = "T" } }] }
            }
        ]
    };

    [Fact]
    public async Task PageExists_is_true_when_query_returns_results()
    {
        _databases.QueryAsync(Arg.Any<string>(), Arg.Any<DatabasesQueryParameters>())
            .Returns(new DatabaseQueryResponse { Results = [new Page()] });

        (await CreateSut().PageExists("title")).ShouldBeTrue();
    }

    [Fact]
    public async Task PageExists_is_false_when_query_returns_nothing()
    {
        _databases.QueryAsync(Arg.Any<string>(), Arg.Any<DatabasesQueryParameters>())
            .Returns(new DatabaseQueryResponse { Results = [] });

        (await CreateSut().PageExists("title")).ShouldBeFalse();
    }

    [Fact]
    public async Task CreatePage_returns_email_id_when_notion_returns_a_url()
    {
        _pages.CreateAsync(Arg.Any<PagesCreateParameters>())
            .Returns(new Page { Url = "https://notion.so/p" });

        (await CreateSut().CreatePage(DraftWithTitleProperty())).ShouldBe("e1");
    }

    [Fact]
    public async Task CreatePage_returns_null_when_notion_returns_no_url()
    {
        _pages.CreateAsync(Arg.Any<PagesCreateParameters>())
            .Returns(new Page { Url = null });

        (await CreateSut().CreatePage(DraftWithTitleProperty())).ShouldBeNull();
    }

    [Fact]
    public async Task CreatePage_throws_when_a_configured_property_is_missing_from_the_draft()
    {
        var draft = DraftWithTitleProperty() with { Properties = [] };

        await Should.ThrowAsync<InvalidOperationException>(() => CreateSut().CreatePage(draft));
        await _pages.DidNotReceive().CreateAsync(Arg.Any<PagesCreateParameters>());
    }
}
