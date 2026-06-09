# EmailIntelligence

Turns newsletter emails into [Notion](https://www.notion.so/) pages. It connects to a mailbox
over IMAP, extracts the readable content of each message, maps it to Notion blocks, and creates a
page per newsletter. Already-processed emails are tracked so they are not imported twice.

It runs as a .NET 10 **Azure Functions** app (isolated worker) on two triggers:

| Trigger | Function | Entry point |
| --- | --- | --- |
| Timer | `NewsletterTimerFunction` | Cron from the `NewsletterSchedule` app setting (default `0 0 */6 * * *`, every 6 hours) |
| HTTP | `NewsletterHttpFunction` | `POST /api/newsletter/run` (requires a function key) |

## Projects

| Project | Responsibility |
| --- | --- |
| `EmailIntelligence.Domain` | Entities, enums, and persistence abstractions — no external dependencies. |
| `EmailIntelligence.Infrastructure` | IMAP (MailKit) and Notion clients, the content-extraction and mapping pipeline, and Cosmos DB persistence. |
| `EmailIntelligence.Functions` | The Azure Functions host, dependency injection wiring, and HTTP/timer triggers. |

## API / OpenAPI

The HTTP API is documented with OpenAPI via the
[Azure Functions OpenAPI extension](https://github.com/Azure/azure-functions-openapi-extension).
Once the app is running these endpoints are available:

| Endpoint | Description |
| --- | --- |
| `GET /api/swagger/ui` | Swagger UI |
| `GET /api/openapi/v3.json` | OpenAPI v3 document |
| `GET /api/swagger.json` | OpenAPI v2 (Swagger) document |

`POST /api/newsletter/run` is protected by a function key. In Swagger UI use **Authorize** and supply
the key (sent as the `x-functions-key` header); with `curl`, pass `?code=<key>` or the same header.

## Configuration

Configuration is read from `appsettings.json` (non-secret structure, e.g. `Notion:Properties`) plus
app settings / user-secrets (secrets). The main sections:

- `Imap` — host, port, credentials, and the folders to read from / move processed mail into.
- `Notion` — `AuthToken`, target `DatabaseId`, and the page `Properties` to populate.
- `Cosmos` — optional. Set `AccountEndpoint` (managed identity) or `ConnectionString` (local/emulator)
  to enable de-duplication of processed emails. When unset, the pipeline still runs.

Locally, put non-secret values in `appsettings.Development.json` and secrets in
[user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets); both are git-ignored.
Host-level settings (`NewsletterSchedule`, `AzureWebJobsStorage`) live in `local.settings.json`.

## Running locally

Prerequisites: the [.NET 10 SDK](https://dotnet.microsoft.com/download), the
[Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local),
and a storage emulator such as [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
(the timer trigger requires storage).

```bash
cd EmailIntelligence.Functions
func start
```

Then open <http://localhost:7071/api/swagger/ui>.

## Docker

`EmailIntelligence.Functions/Dockerfile` builds the app on the Azure Functions isolated .NET 10 base image:

```bash
docker build -t emailintelligence -f EmailIntelligence.Functions/Dockerfile .
```
