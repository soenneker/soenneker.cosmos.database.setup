[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Database.Setup.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Database.Setup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.database.setup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.database.setup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Database.Setup.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Database.Setup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.database.setup/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.database.setup/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Database.Setup

Creates an Azure Cosmos DB database when it does not exist and optionally updates its shared throughput.

## Installation

```bash
dotnet add package Soenneker.Cosmos.Database.Setup
```

## Configuration

```json
{
  "Azure": {
    "Cosmos": {
      "Endpoint": "https://your-account.documents.azure.com:443/",
      "AccountKey": "your-account-key",
      "DatabaseName": "app",
      "DatabaseThroughput": 1000,
      "DatabaseThroughputType": "autoscale",
      "ReplaceDatabaseThroughput": false
    }
  }
}
```

`DatabaseThroughputType` is case-insensitive. The value `autoscale` creates autoscale throughput; every other value selects manual throughput. `DatabaseThroughput` and `DatabaseThroughputType` are required for both `Ensure` overloads because throughput is read from configuration.

When `ReplaceDatabaseThroughput` is `false` or omitted, throughput is supplied only if the database must be created. When it is `true`, the configured throughput is also applied to an existing database after it is resolved.

## Registration and use

```csharp
using Soenneker.Cosmos.Database.Setup.Abstract;
using Soenneker.Cosmos.Database.Setup.Registrars;

services.AddCosmosDatabaseSetupUtilAsSingleton();

ICosmosDatabaseSetupUtil setup = serviceProvider.GetRequiredService<ICosmosDatabaseSetupUtil>();
Microsoft.Azure.Cosmos.Database database = await setup.Ensure(cancellationToken);
```

`Ensure()` reads the endpoint, account key, and database name from `Azure:Cosmos`. To provide those values per call:

```csharp
Microsoft.Azure.Cosmos.Database database = await setup.Ensure(
    endpoint,
    accountKey,
    "app",
    cancellationToken);
```

`AddCosmosDatabaseSetupUtilAsScoped()` is also available. Both registrations add the Cosmos client utility as a singleton.

Transient request timeouts, throttling, HTTP failures, and selected Cosmos service errors are retried five times with exponential backoff and jitter. Authentication, authorization, invalid configuration, exhausted retries, throughput replacement failures, and cancellation propagate to the caller.
