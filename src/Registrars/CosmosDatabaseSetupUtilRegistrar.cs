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
    /// As Singleton
    /// </summary>
    public static void AddCosmosDatabaseSetupUtil(this IServiceCollection services)
    {
        services.AddCosmosClientUtil();
        services.TryAddSingleton<ICosmosDatabaseSetupUtil, CosmosDatabaseSetupUtil>();
    }
}