using EmailIntelligence.Domain.Entities.Drafts.Notion;
using EmailIntelligence.Infrastructure.Clients.Interfaces;

namespace EmailIntelligence.Tests.TestSupport.Fakes;

public sealed class RecordingNotionApiClient : INotionApiClient
{
    private readonly HashSet<string> _existingTitles;

    public RecordingNotionApiClient(IEnumerable<string>? existingTitles = null) =>
        _existingTitles = new HashSet<string>(existingTitles ?? [], StringComparer.Ordinal);

    public List<Page> CreatedDrafts { get; } = [];

    public string? FailCreateForTitle { get; init; }

    public Task<bool> PageExists(string title) =>
        Task.FromResult(_existingTitles.Contains(title));

    public Task<string?> CreatePage(Page draft)
    {
        if (draft.Title == FailCreateForTitle)
            return Task.FromResult<string?>(null);

        CreatedDrafts.Add(draft);
        return Task.FromResult<string?>(draft.EmailId);
    }
}
