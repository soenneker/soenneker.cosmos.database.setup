[![](https://img.shields.io/nuget/v/Soenneker.Cosmos.Database.Setup.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Database.Setup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.database.setup/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.database.setup/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Cosmos.Database.Setup.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Cosmos.Database.Setup/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.cosmos.database.setup/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.cosmos.database.setup/actions/workflows/codeql.yml)

# Soenneker.Cosmos.Database.Setup

A utility library for Azure Cosmos database setup operations Singleton IoC.

## Install

```bash
dotnet add package Soenneker.Cosmos.Database.Setup
```

## Quick start

```csharp
using Soenneker.Cosmos.Database.Setup.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddCosmosDatabaseSetupUtilAsSingleton();
```

Registers Cosmos Database Setup Util with a singleton lifetime.

## What you get

- `ICosmosDatabaseSetupUtil` — A utility library for Azure Cosmos database setup operations Singleton IoC.
- `CosmosDatabaseSetupUtilRegistrar` — A utility library for Azure Cosmos database setup operations.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `ICosmosDatabaseSetupUtil.Ensure(cancellationToken)` | Ensure the database is created. | A task whose result is the requested microsoft.Azure.Cosmos.Database. |
| `ICosmosDatabaseSetupUtil.Ensure(endpoint, accountKey, databaseName, cancellationToken)` | Ensure the database is created. | A task whose result is the requested microsoft.Azure.Cosmos.Database. |
| `CosmosDatabaseSetupUtilRegistrar.AddCosmosDatabaseSetupUtilAsSingleton(services)` | Registers Cosmos Database Setup Util with a singleton lifetime. | The same service collection, so additional registrations can be chained. |
| `CosmosDatabaseSetupUtilRegistrar.AddCosmosDatabaseSetupUtilAsScoped(services)` | Registers Cosmos Database Setup Util with a scoped lifetime. | The same service collection, so additional registrations can be chained. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
- Calls that return a cached or singleton value reuse the same instance until the owning service is disposed.
