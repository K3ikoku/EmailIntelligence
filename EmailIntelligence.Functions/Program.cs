using EmailIntelligence.Domain.Configurations;
using EmailIntelligence.Domain.Entities;
using EmailIntelligence.Infrastructure.Clients;
using EmailIntelligence.Infrastructure.Clients.Interfaces;
using EmailIntelligence.Infrastructure.Services;
using EmailIntelligence.Infrastructure.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

// ASP.NET Core integration so HTTP triggers can use HttpRequest/IActionResult.
builder.ConfigureFunctionsWebApplication();

// Structured, non-secret configuration (e.g. Notion:Properties). Secrets come from app
// settings (Azure) or appsettings.Development.json / user-secrets (local). Trigger
// bindings like %NewsletterSchedule% are resolved by the host from local.settings.json.
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

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

// Pipeline services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailNotionMapperService, EmailNotionMapperService>();
builder.Services.AddTransient<INewsletterPipelineService, NewsletterPipelineService>();
builder.Services.AddTransient<INotionService, NotionService>();
builder.Services.AddTransient<IMailKitClient, MailKitClient>();

// Notion client
builder.Services.AddNotionClient(options =>
    options.AuthToken = builder.Configuration["Notion:AuthToken"]!);
builder.Services.AddSingleton<INotionApiClient, NotionApiClient>();

// Cosmos persistence — registered only when configured, so the email pipeline runs
// without it. Provide Cosmos:AccountEndpoint (managed identity) or Cosmos:ConnectionString
// (local/emulator) to activate IRepository<ProcessedEmail> and the startup initializer.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:AccountEndpoint"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["Cosmos:ConnectionString"]))
{
    builder.Services
        .AddCosmosPersistence(builder.Configuration)
        .AddCosmosContainer<ProcessedEmail>("processed-emails", "/sender");
}

builder.Build().Run();
