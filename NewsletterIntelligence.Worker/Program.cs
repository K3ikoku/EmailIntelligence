using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NewsletterIntelligence;
using NewsletterIntelligence.Domain.Configurations;
using NewsletterIntelligence.Infrastructure.Clients;
using NewsletterIntelligence.Infrastructure.Clients.Interfaces;
using NewsletterIntelligence.Infrastructure.Services;
using NewsletterIntelligence.Infrastructure.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<Worker>();

// Map settings
builder.Services
    .AddOptions<ImapSettings>()
    .Bind(builder.Configuration.GetSection("Imap"))
    .ValidateOnStart();

builder.Services
    .AddOptions<NotionOptions>()
    .Bind(builder.Configuration.GetSection(NotionOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<NotionOptions>, NotionOptionsValidator>();

// Dependency Injection
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IMailKitClient, MailKitClient>();

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ImapSettings>>().Value);

// Notion
builder.Services.AddNotionClient(options =>
    options.AuthToken = builder.Configuration["Notion:AuthToken"]!);
builder.Services.AddSingleton<NotionApiClient>();

// Build api for dev purposes
var app = builder.Build();

app.MapGet("/Newsletter", async (IEmailService emailService) =>
{
    var emails = await emailService.GetAndCleanEmails();
    return Results.Ok(emails);
});

app.Run();