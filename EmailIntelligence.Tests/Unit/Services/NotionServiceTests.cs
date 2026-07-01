using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Domain.Entities.Drafts.Notion;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services;

namespace EmailIntelligence.Tests.Unit.Services;

public class NotionServiceTests
{
    private static Page Draft(string emailId, string title) =>
        new() { EmailId = emailId, Title = title, Blocks = [], Properties = [] };

    private static INotionApiClient ApiWith(out INotionApiClient api)
    {
        api = Substitute.For<INotionApiClient>();
        api.PageExists(Arg.Any<string>()).Returns(false);
        api.CreatePage(Arg.Any<Page>())
            .Returns(call => ((Page)call[0]).EmailId);
        return api;
    }

    [Fact]
    public async Task CreatePage_skips_creation_for_existing_pages_but_still_reports_the_id()
    {
        ApiWith(out var api);
        api.PageExists("Existing title").Returns(true);

        var ids = await new NotionService(api).CreatePage([Draft("e1", "Existing title")]);

        ids.ShouldBe(["e1"]);
        await api.DidNotReceive().CreatePage(Arg.Any<Page>());
    }

    [Fact]
    public async Task CreatePage_creates_new_pages_and_reports_the_returned_id()
    {
        ApiWith(out var api);
        var draft = Draft("e2", "New title");

        var ids = await new NotionService(api).CreatePage([draft]);

        ids.ShouldBe(["e2"]);
        await api.Received(1).CreatePage(draft);
    }

    [Fact]
    public async Task CreatePage_omits_ids_when_creation_fails()
    {
        ApiWith(out var api);
        api.CreatePage(Arg.Any<Page>()).Returns((string?)null);

        var ids = await new NotionService(api).CreatePage([Draft("e3", "New title")]);

        ids.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreatePage_with_no_drafts_creates_nothing()
    {
        ApiWith(out var api);

        (await new NotionService(api).CreatePage([])).ShouldBeEmpty();

        await api.DidNotReceive().PageExists(Arg.Any<string>());
        await api.DidNotReceive().CreatePage(Arg.Any<Page>());
    }

    [Fact]
    public async Task CreatePage_handles_a_mix_of_existing_new_and_failed()
    {
        ApiWith(out var api);
        api.PageExists("T-existing").Returns(true);
        api.CreatePage(Arg.Is<Page>(d => d.Title == "T-fail")).Returns((string?)null);

        var ids = await new NotionService(api).CreatePage(
        [
            Draft("e-existing", "T-existing"),
            Draft("e-new", "T-new"),
            Draft("e-fail", "T-fail")
        ]);

        ids.ShouldBe(["e-existing", "e-new"]);
    }
}
