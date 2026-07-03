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
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddKeyVaultConfiguration();
builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

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
builder.Services.AddTransient<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IValidateOptions<ImapInputConfiguration>, ImapInputConfigurationValidator>();
builder.Services.AddSingleton<IValidateOptions<NotionOutputConfiguration>, NotionOutputConfigurationValidator>();
builder.Services.AddSingleton<IValidateOptions<FeedProfile>, FeedProfileValidator>();

// Notion client
builder.Services.AddNotionClient(options =>
    options.AuthToken = builder.Configuration["Notion:AuthToken"]!);
builder.Services.AddSingleton<INotionApiClient, NotionApiClient>();

// Cosmos persistence
if (!string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:AccountEndpoint"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:ConnectionString"]))
{
    builder.Services
        .AddCosmosPersistence(builder.Configuration)
        .AddCosmosContainer<ConnectorConfiguration>("connectors", "/id")
        .AddCosmosContainer<FeedProfile>("feed-profiles", "/id");

    // Feed profiles are persisted, so the service is only available when Cosmos is configured.
    builder.Services.AddTransient<IFeedProfileService, FeedProfileService>();
}

// OpenAPI 
builder.Services.AddSingleton<IOpenApiConfigurationOptions, OpenApiConfigurationOptions>();
builder.Services.AddSingleton<IOpenApiHttpTriggerContext, OpenApiHttpTriggerContext>();
builder.Services.AddSingleton<IOpenApiTriggerFunction, OpenApiTriggerFunction>();

builder.Build().Run();
