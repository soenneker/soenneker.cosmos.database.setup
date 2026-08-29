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
    /// Registers Cosmos Database Setup Util with a singleton lifetime.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddCosmosDatabaseSetupUtilAsSingleton(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton().TryAddSingleton<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }

    /// <summary>
    /// Registers Cosmos Database Setup Util with a scoped lifetime.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddCosmosDatabaseSetupUtilAsScoped(this IServiceCollection services)
    {
        services.AddCosmosClientUtilAsSingleton().TryAddScoped<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();

        return services;
    }
}
