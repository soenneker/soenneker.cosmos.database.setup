using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Cosmos.Client.Registrars;
using Soenneker.Cosmos.Database.Setup.Abstract;

namespace Soenneker.Cosmos.Database.Setup.Registrars;

/// <summary>
/// A utility library for Azure Cosmos database setup operations
/// </summary>
public static class CosmosDatabaseSetupUtilRegistrar
{
    public static IServiceCollection AddCosmosDatabaseSetupUtilAsScoped(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton();
        services.TryAddScoped<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }

    public static IServiceCollection AddCosmosDatabaseSetupUtilAsSingleton(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton();
        services.TryAddSingleton<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }
}