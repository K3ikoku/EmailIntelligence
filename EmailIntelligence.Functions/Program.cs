using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Entities.Configurations;
using EmailIntelligence.Domain.Entities.Configurations.Notion;
using EmailIntelligence.Domain.Entities.CosmosDocuments;
using EmailIntelligence.Functions.OpenApi;
using EmailIntelligence.Infrastructure.Clients;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Functions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddKeyVaultConfiguration();
builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    var defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
        options.Rules.Remove(defaultRule);
});

// Options
builder.Services.AddOptions<ImapSettings>()
    .Bind(builder.Configuration.GetSection("Imap"));
builder.Services.AddOptions<NotionOptions>()
    .Bind(builder.Configuration.GetSection(NotionOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<NotionOptions>, NotionOptionsValidator>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ImapSettings>>().Value);
builder.Services.AddKeyVaultSecrets(builder.Configuration);

// Pipeline services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailNotionMapperService, EmailNotionMapperService>();
builder.Services.AddTransient<INewsletterPipelineService, NewsletterPipelineService>();
builder.Services.AddTransient<INotionService, NotionService>();
builder.Services.AddTransient<IMailKitClient, MailKitClient>();
builder.Services.AddSingleton<IValidateOptions<ImapInputConfiguration>, ImapInputConfigurationValidator>();
builder.Services.AddSingleton<IValidateOptions<NotionOutputConfiguration>, NotionOutputConfigurationValidator>();
builder.Services.AddSingleton<IValidateOptions<FeedProfile>, FeedProfileValidator>();

// Notion client
var notionAuthToken = builder.Configuration["Notion:AuthToken"];
builder.Services.AddNotionClient(options =>
    options.AuthToken = notionAuthToken ?? throw new InvalidOperationException(
        "Notion:AuthToken is not configured. Set it via user secrets, app settings or Key Vault."));
builder.Services.AddSingleton<INotionApiClient, NotionApiClient>();

// Cosmos persistence
var cosmosConfigured =
    !string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:AccountEndpoint"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:ConnectionString"]);

if (cosmosConfigured)
{
    builder.Services
        .AddCosmosPersistence(builder.Configuration)
        .AddCosmosContainer<ConnectorConfiguration>("connectors", "/id")
        .AddCosmosContainer<FeedProfile>("feed-profiles", "/id");
    builder.Services.AddTransient<IConfigurationService, ConfigurationService>();
    builder.Services.AddTransient<IFeedProfileService, FeedProfileService>();
}
else
{
    // Keep the configuration endpoints resolvable so a call fails with a clear
    // message instead of a cryptic DI activation error.
    builder.Services.AddSingleton<IConfigurationService, UnavailableConfigurationService>();
    builder.Services.AddSingleton<IFeedProfileService, UnavailableFeedProfileService>();
}

// OpenAPI 
builder.Services.AddSingleton<IOpenApiConfigurationOptions, OpenApiConfigurationOptions>();
builder.Services.AddSingleton<IOpenApiHttpTriggerContext, OpenApiHttpTriggerContext>();
builder.Services.AddSingleton<IOpenApiTriggerFunction, OpenApiTriggerFunction>();

var host = builder.Build();

if (!cosmosConfigured)
{
    host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup")
        .LogWarning(
            "Cosmos persistence is not configured (Cosmos:AccountEndpoint / Cosmos:ConnectionString). "
            + "The configuration and feed profile endpoints are unavailable.");
}

host.Run();

file sealed class UnavailableConfigurationService : IConfigurationService
{
    public Task<ConfigurationResult<ImapInputConfiguration>> UpsertImapInputConfigurationAsync(
        ImapInputConfiguration configuration, string password, CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    public Task<ConfigurationResult<NotionOutputConfiguration>> UpsertNotionOutputConfigurationAsync(
        NotionOutputConfiguration configuration, string authToken, CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    public Task<bool> DeleteConnectorAsync(string id, CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    private static InvalidOperationException CosmosNotConfigured() => new(
        "Cosmos persistence is not configured. Set Cosmos:AccountEndpoint or Cosmos:ConnectionString "
        + "to enable the configuration endpoints.");
}

file sealed class UnavailableFeedProfileService : IFeedProfileService
{
    public Task<IEnumerable<FeedProfile>> GetAllFeedProfilesAsync(CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    public Task<ConfigurationResult<FeedProfile>> UpsertFeedProfileAsync(
        FeedProfile feedProfile, CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    public Task<bool> DeleteFeedProfileAsync(string id, CancellationToken cancellationToken = default) =>
        throw CosmosNotConfigured();

    private static InvalidOperationException CosmosNotConfigured() => new(
        "Cosmos persistence is not configured. Set Cosmos:AccountEndpoint or Cosmos:ConnectionString "
        + "to enable the feed profile endpoints.");
}
