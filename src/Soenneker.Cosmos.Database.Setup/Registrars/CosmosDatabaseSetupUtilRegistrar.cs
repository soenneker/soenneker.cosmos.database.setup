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
    /// <summary>
    /// Adds cosmos database setup util as singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddCosmosDatabaseSetupUtilAsSingleton(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton().TryAddSingleton<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }

    /// <summary>
    /// Adds cosmos database setup util as scoped.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The result of the operation.</returns>
    public static IServiceCollection AddCosmosDatabaseSetupUtilAsScoped(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton().TryAddScoped<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }
}