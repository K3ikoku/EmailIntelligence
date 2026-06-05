# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EmailIntelligence.Functions/EmailIntelligence.Functions.csproj", "EmailIntelligence.Functions/"]
COPY ["EmailIntelligence.Infrastructure/EmailIntelligence.Infrastructure.csproj", "EmailIntelligence.Infrastructure/"]
COPY ["EmailIntelligence.Domain/EmailIntelligence.Domain.csproj", "EmailIntelligence.Domain/"]
RUN dotnet restore "EmailIntelligence.Functions/EmailIntelligence.Functions.csproj"
COPY . .
RUN dotnet publish "EmailIntelligence.Functions/EmailIntelligence.Functions.csproj" \
    -c Release -o /home/site/wwwroot /p:UseAppHost=false

# Runtime (Azure Functions isolated .NET 10 host)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build /home/site/wwwroot /home/site/wwwroot
